using Content.Shared.Chat;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Systems;
using Robust.Shared.Player;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    /// <summary>
    /// Starts asynchronous handling of the <paramref name="request"/>.
    /// </summary>
    public void RunTtsRequestHandle(ITtsRequest request)
    {
        RunTaskWithTryCatch(() => HandleTtsRequest(request));
    }

    public Task HandleTtsRequest(ITtsRequest request)
    {
        return request switch
        {
            TtsSayRequest say => HandleSayRequest(say),
            TtsWhisperRequest whisper => HandleWhisperRequest(whisper),
            TtsRadioRequest radio => HandleRadioRequest(radio),
            TtsAnnouncementRequest announcement => HandleAnnouncementRequest(announcement),
            TtsTelepathyRequest telepathy => HandleTelepathyRequest(telepathy),
            TtsVoiceTestRequest voiceTest => HandleVoiceTestRequest(voiceTest),
            _ => throw new NotImplementedException(),
        };
    }

    private async Task HandleSayRequest(TtsSayRequest request)
    {
        if (!TtsEnabled || !IsAnyProviderEnabled() || request.Text.Length > _maxMessageChars)
            return;

        var validReceivers = ToValidReceivers(request.Receivers);
        if (!validReceivers.Any())
            return;

        using var ttsResponce = await ConvertTextToSpeech(request.Text, request.SpeakerData.Voice, TtsKind.Say);
        if (!ttsResponce.TryGetValue(out var audioData))
            return;

        var msg = new PlayTtsMessage(new PlayTtsMessageData
        {
            TtsData = audioData,
            TtsMetadata = new TtsMetadata()
            {
                Provider = request.SpeakerData.Voice.Provider,
                Kind = TtsKind.Say,
                Source = request.SpeakerData.NetSpeaker,
                PlayEntity = request.SpeakerData.NetSpeaker
            }
        });

        foreach (var receiver in validReceivers)
            RaiseNetworkEvent(msg, receiver);
    }

    private async Task HandleWhisperRequest(TtsWhisperRequest request)
    {
        if (!TtsEnabled || !IsAnyProviderEnabled() || request.Text.Length > _maxMessageChars)
            return;

        var textReceivers = new List<ICommonSession>();
        var obfTextReceivers = new List<ICommonSession>();

        var muffledRangeSqr = SharedChatSystem.WhisperMuffledRange * SharedChatSystem.WhisperMuffledRange;
        var clearRangeSqr = SharedChatSystem.WhisperClearRange * SharedChatSystem.WhisperClearRange;
        foreach (var receiver in ToValidReceivers(request.Receivers))
        {
            if (!receiver.AttachedEntity.HasValue)
                continue;

            var xformQuery = GetEntityQuery<TransformComponent>();
            var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(request.SpeakerData.Speaker), xformQuery);

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
            using var ttsResponce = await ConvertTextToSpeech(request.Text, request.SpeakerData.Voice, TtsKind.Whisper);
            if (ttsResponce.TryGetValue(out var audioData))
            {
                var msg = new PlayTtsMessage(new PlayTtsMessageData
                {
                    TtsData = audioData,
                    TtsMetadata = new TtsMetadata()
                    {
                        Provider = request.SpeakerData.Voice.Provider,
                        Kind = TtsKind.Whisper,
                        Source = request.SpeakerData.NetSpeaker,
                        PlayEntity = request.SpeakerData.NetSpeaker
                    }
                });

                foreach (var receiver in textReceivers)
                    RaiseNetworkEvent(msg, receiver);
            }
        }

        if (obfTextReceivers.Count > 0)
        {
            using var obfTtsResponce = await ConvertTextToSpeech(request.ObfuscatedText, request.SpeakerData.Voice, TtsKind.Whisper);
            if (obfTtsResponce.TryGetValue(out var obfAudioData))
            {
                var obfMsg = new PlayTtsMessage(new PlayTtsMessageData
                {
                    TtsData = obfAudioData,
                    TtsMetadata = new TtsMetadata()
                    {
                        Provider = request.SpeakerData.Voice.Provider,
                        Kind = TtsKind.Whisper,
                        Source = request.SpeakerData.NetSpeaker,
                        PlayEntity = request.SpeakerData.NetSpeaker
                    }
                });

                foreach (var receiver in obfTextReceivers)
                    RaiseNetworkEvent(obfMsg, receiver);
            }
        }
    }

    private async Task HandleRadioRequest(TtsRadioRequest request)
    {
        if (!TtsEnabled || !IsAnyProviderEnabled() || request.Text.Length > _maxMessageChars)
            return;

        List<(ICommonSession Session, EntityUid PlayEntity)> validReceivers = [];
        foreach (var receiver in request.Receivers)
        {
            if (!_playerManager.TryGetSessionByEntity(receiver.Actor, out var session))
                continue;

            if (!IsValidReceiver(session))
                continue;

            validReceivers.Add((session, receiver.PlayTarget.EntityId));
        }

        if (validReceivers.Count == 0)
            return;

        using var ttsResponce = await ConvertTextToSpeech(request.Text, request.SpeakerData.Voice, TtsKind.Radio);
        if (!ttsResponce.TryGetValue(out var audioData))
            return;

        foreach (var (session, playEntity) in validReceivers)
        {
            var msg = new PlayTtsMessage(new PlayTtsMessageData
            {
                TtsData = audioData,
                TtsMetadata = new TtsMetadata
                {
                    Provider = request.SpeakerData.Voice.Provider,
                    Kind = TtsKind.Radio,
                    ChannelPrototype = request.ChannelPrototype,
                    Source = request.SpeakerData.NetSpeaker,
                    PlayEntity = GetNetEntity(playEntity)
                }
            });

            RaiseNetworkEvent(msg, session);
        }
    }

    private async Task HandleAnnouncementRequest(TtsAnnouncementRequest request)
    {
        if (!TtsEnabled)
            return;

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

            if (IsAnyProviderEnabled() && request.Text != null && request.Voice != null && request.Text.Length <= _maxAnnounceMessageChars)
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
        if (!TtsEnabled || !IsAnyProviderEnabled() || request.Text.Length > _maxMessageChars)
            return;

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
        if (!TtsEnabled || !IsAnyProviderEnabled() || request.Text.Length > _maxMessageChars)
            return;

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
