using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.TTS;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    private async void HandleEntitySpokeWithLanguage(IEnumerable<EntityUid> receivers, LanguageMessage languageMessage, ServerTtsMetadata meta, string? obfMessage = null)
    {
        Dictionary<string, HashSet<EntityUid>> receiversDict = new();
        foreach (var receiver in receivers)
        {
            string sanitizedMessage;
            if (meta.Kind == TtsKind.Whisper)
                sanitizedMessage = languageMessage.GetObfuscatedMessage(receiver, true);
            else
                sanitizedMessage = languageMessage.GetMessage(receiver, true, false);

            receiversDict.GetOrNew(sanitizedMessage).Add(receiver);
        }

        foreach (var (key, value) in receiversDict)
        {
            HandleEntitySpoke(receivers, key, meta);
        }
    }

    private Dictionary<string, HashSet<EntityUid>> SplitReceiversByLanguangeMessage(IEnumerable<EntityUid> receivers, LanguageMessage languageMessage)
    {
        var result = new Dictionary<string, HashSet<EntityUid>>();
        foreach (var receiver in receivers)
        {

        }
    }

    private async void HandleEntitySpoke(EntityUid listener, string message, TtsContext context, string? obfuscatedMessage = null)
    {
        HandleEntitySpoke([listener], message, context, obfuscatedMessage);
    }

    private async void HandleEntitySpoke(IEnumerable<EntityUid> receivers, string message, ServerTtsMetadata meta, string? obfuscatedMessage = null)
    {
        if (!_isEnabled || message.Length > _maxMessageChars)
            return;

        if (obfuscatedMessage != null)
        {
            HandleWhisperToMany(receivers, message, obfuscatedMessage, meta);
            return;
        }

        HandleSayToMany(receivers, message, context.SpeakerContext);
    }

    private async void HandleSayToMany(TtsSpeakerContext speakerContext, string message)
    {
        var receivers = Filter.Pvs(speakerContext.Speaker).Recipients;
        HandleSayToMany(receivers, message, speakerContext);
    }

    private async void HandleSayToMany(IEnumerable<EntityUid> entities, string message, TtsSpeakerContext speakerContext)
    {
        List<ICommonSession> receivers = [];
        foreach (var entity in entities)
        {
            if (_playerManager.TryGetSessionByEntity(entity, out var receiver) && receiver != null)
                receivers.Add(receiver);
        }

        HandleSayToMany(receivers, message, speakerContext);
    }

    private async void HandleSayToMany(IEnumerable<ICommonSession> receivers, string message, TtsSpeakerContext speakerContext)
    {
        using var ttsResponse = await ConvertTextToSpeech(speakerContext.VoiceId, message, TtsKind.Default);

        if (!ttsResponse.TryGetValue(out var audioData))
            return;

        var ttsMessage = new PlayTtsMessage
        {
            AudioData = audioData,
            Source = speakerContext.NetSpeaker
        };

        foreach (var receiver in receivers)
        {
            HandleSayToOne(receiver, message, speakerContext, ttsMessage);
        }
    }

    private async void HandleSayToOne(EntityUid target, string message, TtsSpeakerContext speakerContext, PlayTtsMessage? ttsMessage = null)
    {
        if (!_playerManager.TryGetSessionByEntity(target, out var receiver))
            return;

        HandleSayToOne(receiver, message, speakerContext, ttsMessage);
    }

    private async void HandleSayToOne(ICommonSession receiver, string message, TtsSpeakerContext speakerContext, PlayTtsMessage? ttsMessage = null)
    {
        if (_sessionsNotToSend.Contains(receiver))
            return;

        if (ttsMessage == null)
        {
            using var ttsResponse = await ConvertTextToSpeech(speakerContext.VoiceId, message, TtsKind.Default);
            if (!ttsResponse.TryGetValue(out var audioData)) return;
            ttsMessage = new PlayTtsMessage
            {
                AudioData = audioData,
                Source = speakerContext.NetSpeaker
            };

            SendTtsMessage(ttsMessage, receiver);
        }
        else
            SendTtsMessage(ttsMessage, receiver);
    }

    private async void HandleWhisperToMany(IEnumerable<EntityUid> entities, string message, string obfMessage, TtsContext context)
    {
        List<ICommonSession> receivers = new();
        foreach (var entity in entities)
        {
            if (_playerManager.TryGetSessionByEntity(entity, out var receiver) && receiver != null)
                receivers.Add(receiver);
        }

        HandleWhisperToMany(receivers, message, obfMessage, context);
    }

    private async void HandleWhisperToMany(IEnumerable<ICommonSession> receivers, string message, string obfMessage, ServerTtsMetadata meta)
    {
        meta.Kind = TtsKind.Whisper;

        PlayTtsMessage? ttsMessage = null;
        using var ttsResponse = await ConvertTextToSpeech(message, meta);
        if (ttsResponse.TryGetValue(out var audioData))
        {
            ttsMessage = new PlayTtsMessage
            {
                AudioData = audioData,
                Source = meta.SpeakerMeta.NetSpeaker,
                Metadata = meta.ToSharedMetadata()
            };
        }

        PlayTtsMessage? obfttsMessage = null;
        using var obfTtsResponse = await ConvertTextToSpeech(obfMessage, meta);
        if (obfTtsResponse.TryGetValue(out var obfAudioData))
        {
            obfttsMessage = new PlayTtsMessage
            {
                AudioData = obfAudioData,
                Source = meta.SpeakerMeta.NetSpeaker,
                Metadata = new(TtsKind.Whisper, meta.ChannelPrototype)
            };
        }

        foreach (var receiver in receivers)
        {
            HandleWhisperToOne(receiver, message, obfMessage, meta, ttsMessage, obfttsMessage);
        }
    }

    private async void HandleWhisperToOne(EntityUid target, string message, string obfMessage, ServerTtsMetadata meta)
    {
        if (!_playerManager.TryGetSessionByEntity(target, out var receiver))
            return;

        HandleWhisperToOne(receiver, message, obfMessage, meta);
    }

    private async void HandleWhisperToOne(
        ICommonSession receiver,
        string message,
        string obfMessage,
        ServerTtsMetadata meta,
        PlayTtsMessage? ttsMessage = null,
        PlayTtsMessage? obfTtsMessage = null)
    {
        if (_sessionsNotToSend.Contains(receiver))
            return;

        if (!receiver.AttachedEntity.HasValue)
            return;

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(meta.SpeakerMeta.Speaker), xformQuery);

        var xform = xformQuery.GetComponent(receiver.AttachedEntity.Value);
        var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

        if (distance > SharedChatSystem.WhisperMuffledRange)
            return;

        meta.Kind = TtsKind.Whisper;

        if (distance > SharedChatSystem.WhisperClearRange)
        {
            if (obfTtsMessage == null)
            {
                using var obfTtsResponse = await ConvertTextToSpeech(obfMessage, meta);
                if (!obfTtsResponse.TryGetValue(out var obfAudioData)) return;
                obfTtsMessage = new PlayTtsMessage
                {
                    AudioData = obfAudioData,
                    Source = meta.SpeakerMeta.NetSpeaker,
                    Metadata = new(TtsKind.Whisper, meta.ChannelPrototype)
                };

                SendTtsMessage(obfTtsMessage, receiver);
            }
            else
                SendTtsMessage(obfTtsMessage, receiver);
        }
        else
        {
            if (ttsMessage == null)
            {
                using var ttsResponse = await ConvertTextToSpeech(message, meta;
                if (!ttsResponse.TryGetValue(out var audioData)) return;
                ttsMessage = new PlayTtsMessage
                {
                    AudioData = audioData,
                    Source = meta.SpeakerMeta.NetSpeaker,
                    Metadata = new(TtsKind.Whisper, meta.ChannelPrototype)
                };

                SendTtsMessage(ttsMessage, receiver);
            }
            else
                SendTtsMessage(ttsMessage, receiver);
        }
    }

    private async void HandleRadio(RadioEventReceiver[] receivers, string message, ServerTtsMetadata meta)
    {
        meta.Kind = TtsKind.Radio;

        using var soundData = await ConvertTextToSpeech(message, meta);
        if (soundData is null)
            return;

        foreach (var receiver in receivers)
        {
            if (!_playerManager.TryGetSessionByEntity(receiver.Actor, out var session) || !soundData.TryGetValue(out var audioData))
                continue;

            SendTtsMessage(new PlayTtsMessage
            {
                AudioData = audioData,
                Source = GetNetEntity(receiver.PlayTarget.EntityId),
                Metadata = meta.ToSharedMetadata(),
            }, session);
        }
    }
}
