using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.SS220.TTS;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    private void InitializeEntitySubscriptions()
    {
        SubscribeLocalEvent<TTSComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
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


        if (!TryGetEntitySpeakerData(args.Source, out var speakerData))
            return;

        var receivers = new List<RadioEventReceiver>();
        foreach (var receiver in args.Receivers)
        {
            var ev = new RadioTtsSendAttemptEvent(args.Channel);
            RaiseLocalEvent(receiver.Actor, ev);

            if (!ev.Cancelled)
                receivers.Add(receiver);
        }

        var request = new TtsRadioRequest()
        {
            SpeakerData = speakerData.Value,
            Text = args.Message,
            ChannelPrototype = args.Channel.ID + args.Frequency?.ToString(),
            Receivers = receivers
        };

        RunTaskWithTryCatch(() => HandleRadioRequest(request));
    }

    private void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        HashSet<EntityUid> receivers = [];
        foreach (var receiver in Filter.Pvs(uid).Recipients)
        {
            if (receiver.AttachedEntity is { } ent)
                receivers.Add(ent);
        }

        if (!TryGetEntitySpeakerData(uid, out var speakerData))
            return;

        ITtsSpokeRequest requestData;
        if (args.ObfuscatedMessage != null)
        {
            requestData = new TtsWhisperRequest()
            {
                SpeakerData = speakerData.Value,
                Text = args.Message,
                ObfuscatedText = args.ObfuscatedMessage,
                Receivers = [.. EntitiesToSessions(receivers)]
            };
        }
        else
        {
            requestData = new TtsSayRequest()
            {
                SpeakerData = speakerData.Value,
                Text = args.Message,
                Receivers = [.. EntitiesToSessions(receivers)]
            };
        }

        if (args.LanguageMessage is { } languageMessage)
            RunTaskWithTryCatch(() => HandleEntitySpokeWithLanguage(requestData, languageMessage));
        else
            RunTaskWithTryCatch(() => HandleSpokeRequest(requestData));
    }

    private void OnAnnouncementSpoke(AnnouncementSpokeEvent args)
    {
        TTSVoicePrototype? voice = null;

        var playSound = args.PlayAudioMask.HasFlag(AudioWithTTSPlayOperation.PlayAudio);
        var playTts = args.PlayAudioMask.HasFlag(AudioWithTTSPlayOperation.PlayTTS) && TryGetVoice(out voice);

        var request = new TtsAnnouncementRequest()
        {
            AnnouncementSound = playSound ? args.AnnouncementSound : null,
            Text = playTts ? args.Message : null,
            Voice = playTts ? voice : null,
            Receivers = [.. args.Source.Recipients]
        };

        RunTaskWithTryCatch(() => HandleAnnouncementRequest(request));

        bool TryGetVoice([NotNullWhen(true)] out TTSVoicePrototype? voice)
        {
            if (_prototypeManager.TryIndex(args.SpokeVoiceId, out voice))
                return true;

            return TryGetPreferredVoice(DefaultAnnouncementVoicePreferences, out voice);
        }
    }

    private void OnTelepathySpoke(TelepathySpokeEvent args)
    {
        if (args.Receivers.Length == 0)
            return;

        if (!TryGetEntitySpeakerData(args.Source, out var speakerData))
            return;

        var request = new TtsTelepathyRequest()
        {
            SpeakerData = speakerData.Value,
            Text = args.Message,
            ChannelPrototype = args.Channel,
            Receivers = [.. EntitiesToSessions(args.Receivers)],
        };

        RunTaskWithTryCatch(() => HandleTelepathyRequest(request));
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
