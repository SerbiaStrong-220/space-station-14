// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.GameTicking;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Commands;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;

    private int _maxMessageChars;
    private int _maxAnnounceMessageChars;
    private bool _isEnabled = false;

    public const float WhisperVoiceVolumeModifier = 0.6f; // how far whisper goes in world units
    public const int WhisperVoiceRange = 6; // how far whisper goes in world units

    [Obsolete("Use tts context instead of referring to it")]
    private readonly ProtoId<TTSVoicePrototype> _fallbackVoiceId = "father_grigori";
    [Obsolete("Use tts context instead of referring to it")]
    private ProtoId<TTSVoicePrototype> _fallbackAnnounceVoiceId = "glados";

    private HashSet<ICommonSession> _sessionsNotToSend = new();

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(CCVars220.MaxCharInTTSAnnounceMessage, x => _maxAnnounceMessageChars = x, true);
        _cfg.OnValueChanged(CCVars220.MaxCharInTTSMessage, x => _maxMessageChars = x, true);
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CCCVars.TTSAnnounceVoiceId, v => _fallbackAnnounceVoiceId = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioReceiveEvent);
        SubscribeLocalEvent<AnnouncementSpokeEvent>(OnAnnouncementSpoke);
        SubscribeLocalEvent<TelepathySpokeEvent>(OnTelepathySpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<TTSComponent, MapInitEvent>(OnInit);

        SubscribeNetworkEvent<RequestGlobalTTSEvent>(OnRequestGlobalTTS);

        // remove if Robust PR for clientCVar subs merged
        SubscribeNetworkEvent<SessionSendTTSMessage>((msg, args) =>
            {
                if (!msg.Value)
                    _sessionsNotToSend.Add(args.SenderSession);
                else
                    _sessionsNotToSend.Remove(args.SenderSession);
            });

        _playerManager.PlayerStatusChanged += (_, x) =>
        {
            if (x.NewStatus == Robust.Shared.Enums.SessionStatus.Disconnected)
                _sessionsNotToSend.Remove(x.Session);
        };
        // end
    }

    // Masks NetManagerMethod for handling client setting
    private void ServerSendMessage(NetMessage message, ICommonSession recipient)
    {
        if (_sessionsNotToSend.Contains(recipient))
            return;

        _netManager.ServerSendMessage(message, recipient.Channel);
    }

    private void OnInit(Entity<TTSComponent> ent, ref MapInitEvent _)
    {

        SetRandomVoice(ent.AsNullable());
    }

    /// <summary>
    /// Set random voice from RandomVoicesList
    /// If RandomVoicesList is null - doesn`t set new voice
    /// </summary>
    private void SetRandomVoice(Entity<TTSComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        var protoId = entity.Comp.RandomVoicesList;

        if (protoId is null)
            return;

        entity.Comp.VoicePrototypeId = _random.Pick(_prototypeManager.Index<RandomVoicesListPrototype>(protoId).VoicesList);
    }

    private void OnRadioReceiveEvent(ref RadioSpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > _maxMessageChars)
            return;

        var context = TtsContextMaker.New(EntityManager, args);

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

    [Obsolete("Use tts context instead of referring to it")]
    private bool GetVoicePrototype(string voiceId, [NotNullWhen(true)] out TTSVoicePrototype? voicePrototype)
    {
        if (!_prototypeManager.TryIndex(voiceId, out voicePrototype))
        {
            return _prototypeManager.Resolve(_fallbackVoiceId, out voicePrototype);
        }

        return true;
    }

    private async void OnAnnouncementSpoke(AnnouncementSpokeEvent args)
    {
        var voice = args.SpokeVoiceId;

        if (string.IsNullOrWhiteSpace(voice))
        {
            if (GetVoicePrototype(_fallbackAnnounceVoiceId, out var protoVoice))
            {
                voice = protoVoice.Speaker;
            }
        }

        var ttsRequired = (args.PlayAudioMask & AudioWithTTSPlayOperation.PlayTTS) == AudioWithTTSPlayOperation.PlayTTS;
        ReferenceCounter<TtsAudioData>.Handle? ttsResponse = default;

        if (_isEnabled && ttsRequired
            && args.Message.Length <= _maxAnnounceMessageChars
            && !string.IsNullOrWhiteSpace(voice))
        {
            ttsResponse = await GenerateTts(args.Message, voice, TtsKind.Announce);
        }

        var message = new MsgPlayAnnounceTts
        {
            AnnouncementSound = args.AnnouncementSound,
            PlayAudioMask = args.PlayAudioMask
        };

        if (ttsRequired && ttsResponse.TryGetValue(out var audioData))
        {
            message.Data = audioData;
        }

        foreach (var session in args.Source.Recipients)
        {
            ServerSendMessage(message, session);
        }

        ttsResponse?.Dispose();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestGlobalTTS(RequestGlobalTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            ev.Text.Length > _maxMessageChars ||
            !GetVoicePrototype(ev.VoiceId, out var protoVoice))
            return;

        using var ttsResponse = await GenerateTts(ev.Text, protoVoice.Speaker, TtsKind.Default);
        if (!ttsResponse.TryGetValue(out var audioData))
            return;

        ServerSendMessage(new MsgPlayTts { Data = audioData }, args.SenderSession);
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        HashSet<EntityUid> receivers = new();
        foreach (var receiver in Filter.Pvs(uid).Recipients)
        {
            if (receiver.AttachedEntity is { } ent)
                receivers.Add(ent);
        }

        var context = TtsContextMaker.New(EntityManager, args);

        if (!context.SpeakerContext.Valid)
            return;

        if (args.LanguageMessage is { } languageMessage)
            HandleEntitySpokeWithLanguage(receivers, languageMessage, context, args.ObfuscatedMessage);
        else
            HandleEntitySpoke(receivers, args.Message, context, args.ObfuscatedMessage);
    }

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
        List<ICommonSession> receivers = new();
        foreach (var entity in entities)
        {
            if (_playerManager.TryGetSessionByEntity(entity, out var receiver) && receiver != null)
                receivers.Add(receiver);
        }

        HandleSayToMany(receivers, message, speakerContext);
    }

    private async void HandleSayToMany(IEnumerable<ICommonSession> receivers, string message, TtsSpeakerContext speakerContext)
    {
        using var ttsResponse = await GenerateTts(message, speakerContext.VoiceId, TtsKind.Default);
        if (!ttsResponse.TryGetValue(out var audioData)) return;
        var ttsMessage = new MsgPlayTts
        {
            Data = audioData,
            SourceUid = speakerContext.NetSpeaker
        };
        foreach (var receiver in receivers)
        {
            HandleSayToOne(receiver, message, speakerContext, ttsMessage);
        }
    }

    private async void HandleSayToOne(EntityUid target, string message, TtsSpeakerContext speakerContext, MsgPlayTts? msgPlayTts = null)
    {
        if (!_playerManager.TryGetSessionByEntity(target, out var receiver))
            return;

        HandleSayToOne(receiver, message, speakerContext, msgPlayTts);
    }

    private async void HandleSayToOne(ICommonSession receiver, string message, TtsSpeakerContext speakerContext, MsgPlayTts? msgPlayTts = null)
    {
        if (_sessionsNotToSend.Contains(receiver))
            return;

        if (msgPlayTts == null)
        {
            using var ttsResponse = await GenerateTts(message, speakerContext.VoiceId, TtsKind.Default);
            if (!ttsResponse.TryGetValue(out var audioData)) return;
            msgPlayTts = new MsgPlayTts
            {
                Data = audioData,
                SourceUid = speakerContext.NetSpeaker
            };

            ServerSendMessage(msgPlayTts, receiver);
        }
        else
            ServerSendMessage(msgPlayTts, receiver);
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
        MsgPlayTts? ttsMessage = null;
        using var ttsResponse = await GenerateTts(message, context.SpeakerContext.VoiceId, TtsKind.Whisper);
        if (ttsResponse.TryGetValue(out var audioData))
        {
            ttsMessage = new MsgPlayTts
            {
                Data = audioData,
                SourceUid = context.SpeakerContext.NetSpeaker,
                Metadata = new(TtsKind.Whisper, context.ChannelPrototype)
            };
        }

        MsgPlayTts? obfttsMessage = null;
        using var obfTtsResponse = await GenerateTts(obfMessage, context.SpeakerContext.VoiceId, TtsKind.Whisper);
        if (obfTtsResponse.TryGetValue(out var obfAudioData))
        {
            obfttsMessage = new MsgPlayTts
            {
                Data = obfAudioData,
                SourceUid = context.SpeakerContext.NetSpeaker,
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
        MsgPlayTts? ttsMessage = null,
        MsgPlayTts? obfTtsMessage = null)
    {
        if (_sessionsNotToSend.Contains(receiver))
            return;

        if (!receiver.AttachedEntity.HasValue)
            return;

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(context.SpeakerContext.Speaker), xformQuery);

        var xform = xformQuery.GetComponent(receiver.AttachedEntity.Value);
        var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

        if (distance > ChatSystem.WhisperMuffledRange)
            return;

        if (distance > ChatSystem.WhisperClearRange)
        {
            if (obfTtsMessage == null)
            {
                using var obfTtsResponse = await GenerateTts(obfMessage, context.SpeakerContext.VoiceId, TtsKind.Whisper);
                if (!obfTtsResponse.TryGetValue(out var obfAudioData)) return;
                obfTtsMessage = new MsgPlayTts
                {
                    Data = obfAudioData,
                    SourceUid = context.SpeakerContext.NetSpeaker,
                    Metadata = new(TtsKind.Whisper, context.ChannelPrototype)
                };
                ServerSendMessage(obfTtsMessage, receiver);
            }
            else
                ServerSendMessage(obfTtsMessage, receiver);
        }
        else
        {
            if (ttsMessage == null)
            {
                using var ttsResponse = await GenerateTts(message, context.SpeakerContext.VoiceId, TtsKind.Whisper);
                if (!ttsResponse.TryGetValue(out var audioData)) return;
                ttsMessage = new MsgPlayTts
                {
                    Data = audioData,
                    SourceUid = context.SpeakerContext.NetSpeaker,
                    Metadata = new(TtsKind.Whisper, context.ChannelPrototype)
                };
                ServerSendMessage(ttsMessage, receiver);
            }
            else
                ServerSendMessage(ttsMessage, receiver);
        }
    }

    private async void HandleRadio(RadioEventReceiver[] receivers, string message, TtsContext context)
    {
        using var soundData = await GenerateTts(message, context.SpeakerContext.VoiceId, TtsKind.Radio);
        if (soundData is null)
            return;

        foreach (var receiver in receivers)
        {
            if (!_playerManager.TryGetSessionByEntity(receiver.Actor, out var session)
                || !soundData.TryGetValue(out var audioData))
                continue;
            ServerSendMessage(new MsgPlayTts
            {
                Data = audioData,
                SourceUid = GetNetEntity(receiver.PlayTarget.EntityId),
                Metadata = new(TtsKind.Radio, context.ChannelPrototype)
            }, session);
        }
    }

    private async void OnTelepathySpoke(TelepathySpokeEvent args)
    {
        if (args.Receivers.Length == 0)
            return;

        var speakerContext = TtsContextMaker.New(EntityManager, args.Source);

        if (!speakerContext.Valid)
            return;

        using var soundData = await GenerateTts(args.Message, speakerContext.VoiceId, TtsKind.Telepathy);
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

            ServerSendMessage(new MsgPlayTts
            {
                Data = audioData,
                // we may need to differ source and entity where we play
                SourceUid = GetNetEntity(receiver),
                Metadata = new(TtsKind.Telepathy, args.Channel is null ? string.Empty : args.Channel)
            }, session);
        }
    }

    private async Task<ReferenceCounter<TtsAudioData>.Handle?> GenerateTts(string text, string speaker, TtsKind kind)
    {
        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "") return default;
            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            var ssmlTraits = SoundTraits.RateFast;
            if (kind == TtsKind.Whisper)
                ssmlTraits |= SoundTraits.PitchVerylow;

            var textSsml = ToSsmlText(textSanitized, ssmlTraits);

            return await _ttsManager.ConvertTextToSpeech(speaker, textSanitized, kind);
        }
        catch (Exception e)
        {
            // Catch TTS exceptions to prevent a server crash.
            Log.Error($"TTS System error: {e.Message}");
        }

        return default;
    }
}

[ByRefEvent]
public record struct TransformSpeakerVoiceEvent(EntityUid Sender, ProtoId<TTSVoicePrototype>? VoiceId) { }
