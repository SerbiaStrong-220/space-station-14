using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.SS220.TTS;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    private void InitializeEntitySubscriptions()
    {
        SubscribeLocalEvent<TTSComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioReceiveEvent);
        SubscribeLocalEvent<AnnouncementSpokeEvent>(OnAnnouncementSpoke);
        SubscribeLocalEvent<TelepathySpokeEvent>(OnTelepathySpoke);
    }

    private void OnInit(Entity<TTSComponent> ent, ref MapInitEvent _)
    {

        SetRandomVoice(ent.AsNullable());
    }

    private void OnRadioReceiveEvent(ref RadioSpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > _maxMessageChars)
            return;

        var context = GetContext(args);

        if (!context.Valid)
            return;

        var receivers = new List<RadioEventReceiver>();

        foreach (var receiver in args.Receivers)
        {
            var ev = new RadioTtsSendAttemptEvent(args.Channel);
            RaiseLocalEvent(receiver.Actor, ev);

            if (!ev.Cancelled)
                receivers.Add(receiver);
        }

        HandleRadio([.. receivers], args.Message, context);
    }

    private async void OnAnnouncementSpoke(AnnouncementSpokeEvent args)
    {
        var voice = args.SpokeVoiceId;

        if (string.IsNullOrWhiteSpace(voice) && !TryGetPreferredVoiceId(DefaultAnnouncementVoicePreferences, out voice))
            return;

        var ttsRequired = (args.PlayAudioMask & AudioWithTTSPlayOperation.PlayTTS) == AudioWithTTSPlayOperation.PlayTTS;
        ReferenceCounter<TtsAudioData>.Handle? ttsResponse = default;

        if (_isEnabled && ttsRequired
            && args.Message.Length <= _maxAnnounceMessageChars
            && !string.IsNullOrWhiteSpace(voice))
        {
            ttsResponse = await ConvertTextToSpeech(voice, args.Message, TtsKind.Announce);
        }

        var message = new PlayAnnounceTtsMessage
        {
            AnnouncementSound = args.AnnouncementSound,
            PlayAudioMask = args.PlayAudioMask
        };

        if (ttsRequired && ttsResponse.TryGetValue(out var audioData))
        {
            message.AudioData = audioData;
        }

        foreach (var session in args.Source.Recipients)
        {
            SendTtsMessage(message, session);
        }

        ttsResponse?.Dispose();
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        HashSet<EntityUid> receivers = [];
        foreach (var receiver in Filter.Pvs(uid).Recipients)
        {
            if (receiver.AttachedEntity is { } ent)
                receivers.Add(ent);
        }

        var context = GetContext(args);

        if (!context.SpeakerContext.Valid)
            return;

        if (args.LanguageMessage is { } languageMessage)
            HandleEntitySpokeWithLanguage(receivers, languageMessage, context, args.ObfuscatedMessage);
        else
            HandleEntitySpoke(receivers, args.Message, context, args.ObfuscatedMessage);
    }

    private async void OnTelepathySpoke(TelepathySpokeEvent args)
    {
        if (args.Receivers.Length == 0)
            return;

        var speakerContext = GetSpeakerContext(args.Source);

        if (!speakerContext.Valid)
            return;

        using var soundData = await ConvertTextToSpeech(speakerContext.VoiceId, args.Message, TtsKind.Telepathy);
        if (soundData is null)
            return;

        foreach (var receiver in args.Receivers)
        {
            if (!_playerManager.TryGetSessionByEntity(receiver, out var session)
                || !soundData.TryGetValue(out var audioData))
                continue;

            // Double check to prevent pointless event raising
            if (_sessionsNotToSend.Contains(session))
                continue;

            var ev = new TelepathyTtsSendAttemptEvent(receiver, args.Channel);
            RaiseLocalEvent(receiver, ev);

            if (ev.Cancelled)
                continue;

            SendTtsMessage(new PlayTtsMessage
            {
                AudioData = audioData,
                // we may need to differ source and entity where we play
                Source = GetNetEntity(receiver),
                Metadata = new(TtsKind.Telepathy, args.Channel is null ? string.Empty : args.Channel)
            }, session);
        }
    }

    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!_isEnabled)
            return;

        args.Message = args.Message.Replace("+", "");
    }
}

[ByRefEvent]
public record struct TransformSpeakerVoiceEvent(EntityUid Sender, ProtoId<TTSVoicePrototype>? VoiceId) { }
