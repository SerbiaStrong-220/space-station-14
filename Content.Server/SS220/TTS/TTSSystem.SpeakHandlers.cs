using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.TTS;
using Robust.Shared.Player;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    private async void HandleEntitySpokeWithLanguage(IEnumerable<EntityUid> receivers, LanguageMessage languageMessage, TtsContext context, string? obfuscatedMessage = null)
    {
        Dictionary<string, (HashSet<EntityUid>, string?)> messageListenersDict = new();
        foreach (var receiver in receivers)
        {
            string sanitizedMessage = languageMessage.GetMessage(receiver, true, false);
            if (obfuscatedMessage != null)
                obfuscatedMessage = languageMessage.GetObfuscatedMessage(receiver, true);

            if (messageListenersDict.TryGetValue(sanitizedMessage, out var listeners))
                listeners.Item1.Add(receiver);
            else
                messageListenersDict[sanitizedMessage] = ([receiver], obfuscatedMessage);
        }

        foreach (var (key, value) in messageListenersDict)
        {
            HandleEntitySpoke(value.Item1, key, context, value.Item2);
        }
    }

    private async void HandleEntitySpoke(EntityUid listener, string message, TtsContext context, string? obfuscatedMessage = null)
    {
        HandleEntitySpoke([listener], message, context, obfuscatedMessage);
    }

    private async void HandleEntitySpoke(IEnumerable<EntityUid> receivers, string message, TtsContext context, string? obfuscatedMessage = null)
    {
        if (!_isEnabled || message.Length > _maxMessageChars)
            return;

        if (obfuscatedMessage != null)
        {
            HandleWhisperToMany(receivers, message, obfuscatedMessage, context);
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

    private async void HandleWhisperToMany(IEnumerable<ICommonSession> receivers, string message, string obfMessage, TtsContext context)
    {
        PlayTtsMessage? ttsMessage = null;
        using var ttsResponse = await ConvertTextToSpeech(context.SpeakerContext.VoiceId, message, TtsKind.Whisper);
        if (ttsResponse.TryGetValue(out var audioData))
        {
            ttsMessage = new PlayTtsMessage
            {
                AudioData = audioData,
                Source = context.SpeakerContext.NetSpeaker,
                Metadata = new(TtsKind.Whisper, context.ChannelPrototype)
            };
        }

        PlayTtsMessage? obfttsMessage = null;
        using var obfTtsResponse = await ConvertTextToSpeech(context.SpeakerContext.VoiceId, obfMessage, TtsKind.Whisper);
        if (obfTtsResponse.TryGetValue(out var obfAudioData))
        {
            obfttsMessage = new PlayTtsMessage
            {
                AudioData = obfAudioData,
                Source = context.SpeakerContext.NetSpeaker,
                Metadata = new(TtsKind.Whisper, context.ChannelPrototype)
            };
        }

        foreach (var receiver in receivers)
        {
            HandleWhisperToOne(receiver, message, obfMessage, context, ttsMessage, obfttsMessage);
        }
    }

    private async void HandleWhisperToOne(EntityUid target, string message, string obfMessage, TtsContext context)
    {
        if (!_playerManager.TryGetSessionByEntity(target, out var receiver))
            return;

        HandleWhisperToOne(receiver, message, obfMessage, context);
    }

    private async void HandleWhisperToOne(
        ICommonSession receiver,
        string message,
        string obfMessage,
        TtsContext context,
        PlayTtsMessage? ttsMessage = null,
        PlayTtsMessage? obfTtsMessage = null)
    {
        if (_sessionsNotToSend.Contains(receiver))
            return;

        if (!receiver.AttachedEntity.HasValue)
            return;

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(context.SpeakerContext.Speaker), xformQuery);

        var xform = xformQuery.GetComponent(receiver.AttachedEntity.Value);
        var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

        if (distance > SharedChatSystem.WhisperMuffledRange)
            return;

        if (distance > SharedChatSystem.WhisperClearRange)
        {
            if (obfTtsMessage == null)
            {
                using var obfTtsResponse = await ConvertTextToSpeech(context.SpeakerContext.VoiceId, obfMessage, TtsKind.Whisper);
                if (!obfTtsResponse.TryGetValue(out var obfAudioData)) return;
                obfTtsMessage = new PlayTtsMessage
                {
                    AudioData = obfAudioData,
                    Source = context.SpeakerContext.NetSpeaker,
                    Metadata = new(TtsKind.Whisper, context.ChannelPrototype)
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
                using var ttsResponse = await ConvertTextToSpeech(context.SpeakerContext.VoiceId, message, TtsKind.Whisper);
                if (!ttsResponse.TryGetValue(out var audioData)) return;
                ttsMessage = new PlayTtsMessage
                {
                    AudioData = audioData,
                    Source = context.SpeakerContext.NetSpeaker,
                    Metadata = new(TtsKind.Whisper, context.ChannelPrototype)
                };

                SendTtsMessage(ttsMessage, receiver);
            }
            else
                SendTtsMessage(ttsMessage, receiver);
        }
    }

    private async void HandleRadio(RadioEventReceiver[] receivers, string message, TtsContext context)
    {
        using var soundData = await ConvertTextToSpeech(context.SpeakerContext.VoiceId, message, TtsKind.Radio);
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
                Metadata = new(TtsKind.Radio, context.ChannelPrototype)
            }, session);
        }
    }
}
