using Content.Shared.Chat;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.TTS;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    public void RunTtsRequestHandle(ITtsRequest request)
    {
        RunTaskWithTryCatch(() => HandleTtsRequest(request));
    }

    public Task HandleTtsRequest(ITtsRequest request)
    {
        return request switch
        {
            ITtsSpokeRequest spoke => HandleSpokeRequest(spoke),
            TtsRadioRequest radio => HandleRadioRequest(radio),
            TtsAnnouncementRequest announcement => HandleAnnouncementRequest(announcement),
            TtsTelepathyRequest telepathy => HandleTelepathyRequest(telepathy),
            _ => throw new NotImplementedException(),
        };
    }

    private Task HandleSpokeRequest(ITtsSpokeRequest request)
    {
        return request switch
        {
            TtsSayRequest say => HandleSayRequest(say),
            TtsWhisperRequest whisper => HandleWhisperRequest(whisper),
            _ => throw new NotImplementedException()
        };
    }

    private async Task HandleSayRequest(TtsSayRequest sayRequest)
    {
        var validReceivers = ToValidReceivers(sayRequest.Receivers);
        if (!validReceivers.Any())
            return;

        using var ttsResponce = await ConvertTextToSpeech(sayRequest.Text, sayRequest.SpeakerData.Voice, TtsKind.Say);
        if (!ttsResponce.TryGetValue(out var audioData))
            return;

        var msg = new PlayTtsMessage(new PlayTtsMessageData
        {
            TtsData = audioData,
            TtsMetadata = new TtsMetadata()
            {
                Provider = sayRequest.SpeakerData.Voice.Provider,
                Kind = TtsKind.Say,
                Source = sayRequest.SpeakerData.NetSpeaker,
                PlayEntity = sayRequest.SpeakerData.NetSpeaker
            }
        });

        foreach (var receiver in validReceivers)
            RaiseNetworkEvent(msg, receiver);
    }

    private async Task HandleWhisperRequest(TtsWhisperRequest whisperRequest)
    {
        var textReceivers = new List<ICommonSession>();
        var obfTextReceivers = new List<ICommonSession>();

        var muffledRangeSqr = SharedChatSystem.WhisperMuffledRange * SharedChatSystem.WhisperMuffledRange;
        var clearRangeSqr = SharedChatSystem.WhisperClearRange * SharedChatSystem.WhisperClearRange;
        foreach (var receiver in ToValidReceivers(whisperRequest.Receivers))
        {
            if (!receiver.AttachedEntity.HasValue)
                continue;

            var xformQuery = GetEntityQuery<TransformComponent>();
            var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(whisperRequest.SpeakerData.Speaker), xformQuery);

            var xform = xformQuery.GetComponent(receiver.AttachedEntity.Value);
            var distanceSqr = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();

            if (distanceSqr > muffledRangeSqr)
                continue;

            if (distanceSqr > clearRangeSqr)
                obfTextReceivers.Add(receiver);
            else
                textReceivers.Add(receiver);
        }

        if (textReceivers.Count > 0)
        {
            using var ttsResponce = await ConvertTextToSpeech(whisperRequest.Text, whisperRequest.SpeakerData.Voice, TtsKind.Whisper);
            if (ttsResponce.TryGetValue(out var audioData))
            {
                var msg = new PlayTtsMessage(new PlayTtsMessageData
                {
                    TtsData = audioData,
                    TtsMetadata = new TtsMetadata()
                    {
                        Provider = whisperRequest.SpeakerData.Voice.Provider,
                        Kind = TtsKind.Whisper,
                        Source = whisperRequest.SpeakerData.NetSpeaker,
                        PlayEntity = whisperRequest.SpeakerData.NetSpeaker
                    }
                });

                foreach (var receiver in textReceivers)
                    RaiseNetworkEvent(msg, receiver);
            }
        }

        if (obfTextReceivers.Count > 0)
        {
            using var obfTtsResponce = await ConvertTextToSpeech(whisperRequest.ObfuscatedText, whisperRequest.SpeakerData.Voice, TtsKind.Whisper);
            if (obfTtsResponce.TryGetValue(out var obfAudioData))
            {
                var obfMsg = new PlayTtsMessage(new PlayTtsMessageData
                {
                    TtsData = obfAudioData,
                    TtsMetadata = new TtsMetadata()
                    {
                        Provider = whisperRequest.SpeakerData.Voice.Provider,
                        Kind = TtsKind.Whisper,
                        Source = whisperRequest.SpeakerData.NetSpeaker,
                        PlayEntity = whisperRequest.SpeakerData.NetSpeaker
                    }
                });

                foreach (var receiver in obfTextReceivers)
                    RaiseNetworkEvent(obfMsg, receiver);
            }
        }
    }

    private async Task HandleRadioRequest(TtsRadioRequest radioData)
    {
        List<(ICommonSession Session, EntityUid PlayEntity)> validReceivers = [];
        foreach (var receiver in radioData.Receivers)
        {
            if (!_playerManager.TryGetSessionByEntity(receiver.Actor, out var session))
                continue;

            if (!IsValidReceiver(session))
                continue;

            validReceivers.Add((session, receiver.PlayTarget.EntityId));
        }

        if (validReceivers.Count == 0)
            return;

        using var ttsResponce = await ConvertTextToSpeech(radioData.Text, radioData.SpeakerData.Voice, TtsKind.Radio);
        if (!ttsResponce.TryGetValue(out var audioData))
            return;

        foreach (var (session, playEntity) in validReceivers)
        {
            var msg = new PlayTtsMessage(new PlayTtsMessageData
            {
                TtsData = audioData,
                TtsMetadata = new TtsMetadata
                {
                    Provider = radioData.SpeakerData.Voice.Provider,
                    Kind = TtsKind.Radio,
                    ChannelPrototype = radioData.ChannelPrototype,
                    Source = radioData.SpeakerData.NetSpeaker,
                    PlayEntity = GetNetEntity(playEntity)
                }
            });

            RaiseNetworkEvent(msg, session);
        }
    }

    private async Task HandleAnnouncementRequest(TtsAnnouncementRequest request)
    {
        var validReceivers = ToValidReceivers(request.Receivers);
        if (!validReceivers.Any())
            return;

        TtsResponse.Reference? responce = null;
        try
        {
            var ttsMeta = new TtsMetadata()
            {
                Kind = TtsKind.Announce,
                Provider = request.Voice?.Provider,
            };

            var msg = new PlayTtsMessage();

            if (request.AnnouncementSound != null)
            {
                msg.Datas.Add(new PlayTtsMessageData
                {
                    TtsData = new TtsSoundSpecifierData(request.AnnouncementSound),
                    TtsMetadata = ttsMeta
                });
            }

            if (request.Text != null && request.Voice != null)
            {
                responce = await ConvertTextToSpeech(request.Text, request.Voice, TtsKind.Announce);
                if (responce.TryGetValue(out var audioData))
                {
                    msg.Datas.Add(new PlayTtsMessageData
                    {
                        TtsData = audioData,
                        TtsMetadata = ttsMeta
                    });
                }
            }

            if (msg.Datas.Count == 0)
                return;

            foreach (var receiver in validReceivers)
                RaiseNetworkEvent(msg, receiver);
        }
        finally
        {
            responce?.Dispose();
        }
    }

    private async Task HandleTelepathyRequest(TtsTelepathyRequest request)
    {
        var validReceivers = ToValidReceivers(request.Receivers)
            .Where(x =>
            {
                if (x.AttachedEntity is not { } enitity)
                    return false;

                var ev = new TelepathyTtsSendAttemptEvent(enitity, request.ChannelPrototype);
                RaiseLocalEvent(enitity, ev);

                return !ev.Cancelled;
            });

        if (!validReceivers.Any())
            return;

        using var responce = await ConvertTextToSpeech(request.Text, request.SpeakerData.Voice, TtsKind.Telepathy);
        if (!responce.TryGetValue(out var audioData))
            return;

        var msg = new PlayTtsMessage(new PlayTtsMessageData
        {
            TtsData = audioData,
            TtsMetadata = new TtsMetadata()
            {
                Kind = TtsKind.Telepathy,
                Provider = request.SpeakerData.Voice.Provider,
                Source = request.SpeakerData.NetSpeaker,
                ChannelPrototype = request.ChannelPrototype,
            }
        });

        foreach (var receiver in validReceivers)
            RaiseNetworkEvent(msg, receiver);
    }

    private async Task HandleVoiceTestRequest(TtsVoiceTestRequest request)
    {
        var validReceivers = ToValidReceivers(request.Receivers);
        if (!validReceivers.Any())
            return;

        using var responce = await ConvertTextToSpeech(request.Text, request.Voice, TtsKind.VoiceTest);
        if (!responce.TryGetValue(out var audioData))
            return;

        var msg = new PlayTtsMessage(new PlayTtsMessageData
        {
            TtsData = audioData,
            TtsMetadata = new TtsMetadata()
            {
                Kind = TtsKind.VoiceTest,
                Provider = request.Voice.Provider
            }
        });

        foreach (var receiver in validReceivers)
            RaiseNetworkEvent(msg, receiver);
    }

    private static IEnumerable<ITtsSpokeRequest> SplitRequestByLanguage(ITtsSpokeRequest request, LanguageMessage languageMessage)
    {
        Dictionary<string, ITtsSpokeRequest> result = [];

        foreach (var receiver in request.Receivers)
        {
            if (receiver.AttachedEntity is not { } entity)
                continue;

            var sanitizedText = languageMessage.GetMessage(entity, true, colored: false);

            if (result.TryGetValue(sanitizedText, out var exist))
                exist.Receivers.Add(receiver);
            else
            {
                ITtsSpokeRequest newRequest;
                switch (request)
                {
                    case TtsSayRequest sayRequest:
                        newRequest = new TtsSayRequest()
                        {
                            SpeakerData = sayRequest.SpeakerData,
                            Text = sanitizedText,
                            Receivers = [receiver]
                        };
                        break;

                    case TtsWhisperRequest whisperRequest:
                        newRequest = new TtsWhisperRequest()
                        {
                            SpeakerData = whisperRequest.SpeakerData,
                            Text = sanitizedText,
                            ObfuscatedText = languageMessage.GetObfuscatedMessage(entity, true),
                            Receivers = [receiver]
                        };
                        break;

                    default:
                        continue;
                }

                result[sanitizedText] = newRequest;
            }
        }

        return result.Values;
    }
}
