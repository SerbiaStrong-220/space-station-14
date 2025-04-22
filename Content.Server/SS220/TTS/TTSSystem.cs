// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Server.VoiceMask;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Inventory;
using Content.Shared.SS220.TTS;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Network;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using System.Linq;
using Content.Server.SS220.Language;
using Content.Shared.SS220.Language.Systems;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly LanguageSystem _language = default!;

    private ISawmill _sawmill = default!;

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled = false;
    private string _voiceId = "glados";
    public const float WhisperVoiceVolumeModifier = 0.6f; // how far whisper goes in world units
    public const int WhisperVoiceRange = 6; // how far whisper goes in world units

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CCCVars.TTSAnnounceVoiceId, v => _voiceId = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioReceiveEvent);
        SubscribeLocalEvent<AnnouncementSpokeEvent>(OnAnnouncementSpoke);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<TTSComponent, MapInitEvent>(OnInit);

        SubscribeNetworkEvent<RequestGlobalTTSEvent>(OnRequestGlobalTTS);

        _sawmill = _log.GetSawmill("TTSSystem");
    }

    private void OnInit(Entity<TTSComponent> ent, ref MapInitEvent args)
    {
        // Set random voice from RandomVoicesList
        // If RandomVoicesList is null - doesn`t set new voice
        SetRandomVoice(ent);
    }

    private void OnRadioReceiveEvent(RadioSpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        if (!TryComp(args.Source, out TTSComponent? senderComponent))
            return;

        var voiceId = senderComponent.VoicePrototypeId;
        if (voiceId == null)
            return;

        if (TryGetVoiceMaskUid(args.Source, out var maskUid))
        {
            var voiceEv = new TransformSpeakerVoiceEvent(maskUid.Value, voiceId);
            RaiseLocalEvent(maskUid.Value, voiceEv);
            voiceId = voiceEv.VoiceId;
        }

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        HandleRadio(args.Receivers, args.Message, protoVoice.Speaker);
    }

    private bool GetVoicePrototype(string voiceId, [NotNullWhen(true)] out TTSVoicePrototype? voicePrototype)
    {
        if (!_prototypeManager.TryIndex(voiceId, out voicePrototype))
        {
            return _prototypeManager.TryIndex("father_grigori", out voicePrototype);
        }

        return true;
    }

    private void SetRandomVoice(EntityUid uid, TTSComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var protoId = comp.RandomVoicesList;
        if (protoId is null)
            return;

        comp.VoicePrototypeId = _random.Pick(_prototypeManager.Index<RandomVoicesListPrototype>(protoId).VoicesList);
    }

    private async void OnAnnouncementSpoke(AnnouncementSpokeEvent args)
    {
        var voice = args.SpokeVoiceId;

        if (string.IsNullOrWhiteSpace(voice))
        {
            if (GetVoicePrototype(_voiceId, out var protoVoice))
            {
                voice = protoVoice.Speaker;
            }
        }

        ReferenceCounter<TtsAudioData>.Handle? ttsResponse = default;

        if (_isEnabled
            && args.Message.Length <= MaxMessageChars * 2
            && !string.IsNullOrWhiteSpace(voice))
        {
            ttsResponse = await GenerateTts(args.Message, voice, TtsKind.Announce);
        }

        var message = new MsgPlayAnnounceTts
        {
            AnnouncementSound = args.AnnouncementSound,
            AnnouncementParams = args.AnnouncementSoundParams,
        };

        if (ttsResponse.TryGetValue(out var audioData))
        {
            message.Data = audioData;
        }

        foreach (var session in args.Source.Recipients)
        {
            _netManager.ServerSendMessage(message, session.Channel);
        }

        ttsResponse?.Dispose();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    public bool TryGetVoiceMaskUid(EntityUid maskCarrier, [NotNullWhen(true)] out EntityUid? maskUid)
    {
        maskUid = null;
        if (!_inventory.TryGetContainerSlotEnumerator(maskCarrier, out var carrierSlot, SlotFlags.MASK))
            return false;

        while (carrierSlot.NextItem(out var itemUid, out var itemSlot))
        {
            if (HasComp<VoiceMaskComponent>(itemUid))
            {
                maskUid = itemUid;
                return true;
            }
        }
        return false;
    }

    private async void OnRequestGlobalTTS(RequestGlobalTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            ev.Text.Length > MaxMessageChars ||
            !GetVoicePrototype(ev.VoiceId, out var protoVoice))
            return;

        using var ttsResponse = await GenerateTts(ev.Text, protoVoice.Speaker, TtsKind.Default);
        if (!ttsResponse.TryGetValue(out var audioData))
            return;

        _netManager.ServerSendMessage(new MsgPlayTts { Data = audioData }, args.SenderSession.Channel);
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        HashSet<EntityUid> receivers = new();
        foreach (var receiver in Filter.Pvs(uid).Recipients)
        {
            if (receiver.AttachedEntity is { } ent)
                receivers.Add(ent);
        }

        if (args.LanguageMessage is { } languageMessage)
            HandleEntitySpokeWithLanguage(uid, receivers, languageMessage, args.IsRadio, args.ObfuscatedMessage);
        else
            HandleEntitySpoke(uid, receivers, args.Message, args.IsRadio, args.ObfuscatedMessage);
    }

    private async void HandleEntitySpokeWithLanguage(EntityUid source, IEnumerable<EntityUid> receivers, LanguageMessage languageMessage, bool isRadio, string? obfuscatedMessage = null)
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
            HandleEntitySpoke(source, value.Item1, key, isRadio, value.Item2);
        }
    }

    private async void HandleEntitySpoke(EntityUid source, EntityUid listener, string message, bool isRadio, string? obfuscatedMessage = null)
    {
        HandleEntitySpoke(source, [listener], message, isRadio, obfuscatedMessage);
    }

    private async void HandleEntitySpoke(EntityUid source, IEnumerable<EntityUid> receivers, string message, bool isRadio, string? obfuscatedMessage = null)
    {
        if (!_isEnabled ||
            message.Length > MaxMessageChars ||
            !TryComp<TTSComponent>(source, out var component) ||
            component.VoicePrototypeId == null)
            return;

        var voiceId = component.VoicePrototypeId;
        if (TryGetVoiceMaskUid(source, out var maskUid))
        {
            var voiceEv = new TransformSpeakerVoiceEvent(maskUid.Value, voiceId);
            RaiseLocalEvent(maskUid.Value, voiceEv);
            voiceId = voiceEv.VoiceId;
        }

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        if (obfuscatedMessage != null)
        {
            HandleWhisperToMany(source, receivers, message, obfuscatedMessage, protoVoice.Speaker, isRadio);
            return;
        }

        HandleSayToMany(source, receivers, message, protoVoice.Speaker);
    }

    private async void HandleSayToMany(EntityUid source, string message, string speaker)
    {
        var receivers = Filter.Pvs(source).Recipients;
        HandleSayToMany(source, receivers, message, speaker);
    }

    private async void HandleSayToMany(EntityUid source, IEnumerable<EntityUid> entities, string message, string speaker)
    {
        List<ICommonSession> receivers = new();
        foreach (var entity in entities)
        {
            if (_playerManager.TryGetSessionByEntity(entity, out var receiver) && receiver != null)
                receivers.Add(receiver);
        }

        HandleSayToMany(source, receivers, message, speaker);
    }

    private async void HandleSayToMany(EntityUid source, IEnumerable<ICommonSession> receivers, string message, string speaker)
    {
        using var ttsResponse = await GenerateTts(message, speaker, TtsKind.Default);
        if (!ttsResponse.TryGetValue(out var audioData)) return;
        var ttsMessage = new MsgPlayTts
        {
            Data = audioData,
            SourceUid = GetNetEntity(source),
        };
        foreach (var receiver in receivers)
        {
            HandleSayToOne(source, receiver, message, speaker, ttsMessage);
        }
    }

    private async void HandleSayToOne(EntityUid source, EntityUid target, string message, string speaker, MsgPlayTts? msgPlayTts = null)
    {
        if (!_playerManager.TryGetSessionByEntity(target, out var receiver))
            return;

        HandleSayToOne(source, receiver, message, speaker, msgPlayTts);
    }

    private async void HandleSayToOne(EntityUid source, ICommonSession receiver, string message, string speaker, MsgPlayTts? msgPlayTts = null)
    {
        if (msgPlayTts == null)
        {
            using var ttsResponse = await GenerateTts(message, speaker, TtsKind.Default);
            if (!ttsResponse.TryGetValue(out var audioData)) return;
            msgPlayTts = new MsgPlayTts
            {
                Data = audioData,
                SourceUid = GetNetEntity(source),
            };

            _netManager.ServerSendMessage(msgPlayTts, receiver.Channel);
        }
        else
            _netManager.ServerSendMessage(msgPlayTts, receiver.Channel);
    }

    private async void HandleWhisperToMany(EntityUid source, IEnumerable<EntityUid> entities, string message, string obfMessage, string speaker, bool isRadio)
    {
        List<ICommonSession> receivers = new();
        foreach (var entity in entities)
        {
            if (_playerManager.TryGetSessionByEntity(entity, out var receiver) && receiver != null)
                receivers.Add(receiver);
        }

        HandleWhisperToMany(source, receivers, message, obfMessage, speaker, isRadio);
    }

    private async void HandleWhisperToMany(EntityUid source, IEnumerable<ICommonSession> receivers, string message, string obfMessage, string speaker, bool isRadio)
    {
        using var ttsResponse = await GenerateTts(message, speaker, TtsKind.Whisper);
        if (!ttsResponse.TryGetValue(out var audioData)) return;
        var ttsMessage = new MsgPlayTts
        {
            Data = audioData,
            SourceUid = GetNetEntity(source),
            Kind = TtsKind.Whisper
        };

        using var obfTtsResponse = await GenerateTts(obfMessage, speaker, TtsKind.Whisper);
        if (!obfTtsResponse.TryGetValue(out var obfAudioData)) return;
        var obfttsMessage = new MsgPlayTts
        {
            Data = obfAudioData,
            SourceUid = GetNetEntity(source),
            Kind = TtsKind.Whisper
        };

        foreach (var receiver in receivers)
        {
            HandleWhisperToOne(source, receiver, message, obfMessage, speaker, isRadio, ttsMessage, obfttsMessage);
        }
    }

    private async void HandleWhisperToOne(EntityUid uid, EntityUid target, string message, string obfMessage, string speaker, bool isRadio)
    {
        if (!_playerManager.TryGetSessionByEntity(target, out var receiver))
            return;

        HandleWhisperToOne(uid, receiver, message, obfMessage, speaker, isRadio);
    }

    private async void HandleWhisperToOne(EntityUid source,
        ICommonSession receiver,
        string message,
        string obfMessage,
        string speaker,
        bool isRadio,
        MsgPlayTts? ttsMessage = null,
        MsgPlayTts? obfTtsMessage = null)
    {
        if (!receiver.AttachedEntity.HasValue)
            return;

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(source), xformQuery);

        var xform = xformQuery.GetComponent(receiver.AttachedEntity.Value);
        var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

        if (distance > ChatSystem.WhisperMuffledRange)
            return;

        if (distance > ChatSystem.WhisperClearRange)
        {
            if (obfTtsMessage == null)
            {
                using var obfTtsResponse = await GenerateTts(obfMessage, speaker, TtsKind.Whisper);
                if (!obfTtsResponse.TryGetValue(out var obfAudioData)) return;
                obfTtsMessage = new MsgPlayTts
                {
                    Data = obfAudioData,
                    SourceUid = GetNetEntity(source),
                    Kind = TtsKind.Whisper
                };
                _netManager.ServerSendMessage(obfTtsMessage, receiver.Channel);
            }
            else
                _netManager.ServerSendMessage(obfTtsMessage, receiver.Channel);
        }
        else
        {
            if (ttsMessage == null)
            {
                using var ttsResponse = await GenerateTts(message, speaker, TtsKind.Whisper);
                if (!ttsResponse.TryGetValue(out var audioData)) return;
                ttsMessage = new MsgPlayTts
                {
                    Data = audioData,
                    SourceUid = GetNetEntity(source),
                    Kind = TtsKind.Whisper
                };
                _netManager.ServerSendMessage(ttsMessage, receiver.Channel);
            }
            else
                _netManager.ServerSendMessage(ttsMessage, receiver.Channel);
        }
    }

    private async void HandleLanguageRadio(RadioEventReceiver[] receivers, LanguageMessage languageMessage, string speaker)
    {
        Dictionary<string, List<RadioEventReceiver>> splitedByHearedMessage = new();
        foreach (var receiver in receivers)
        {
            var hearedMessage = languageMessage.GetMessage(receiver.Actor, true, false);
            if (splitedByHearedMessage.TryGetValue(hearedMessage, out var value))
                value.Add(receiver);
            else
                splitedByHearedMessage[hearedMessage] = [receiver];
        }

        foreach (var (message, newReceivers) in splitedByHearedMessage)
            HandleRadio(receivers, message, speaker);
    }

    private async void HandleRadio(RadioEventReceiver[] receivers, string message, string speaker)
    {
        using var soundData = await GenerateTts(message, speaker, TtsKind.Radio);
        if (soundData is null)
            return;

        foreach (var receiver in receivers)
        {
            if (!_playerManager.TryGetSessionByEntity(receiver.Actor, out var session)
                || !soundData.TryGetValue(out var audioData))
                continue;
            _netManager.ServerSendMessage(new MsgPlayTts
            {
                Data = audioData,
                SourceUid = GetNetEntity(receiver.PlayTarget.EntityId),
                Kind = TtsKind.Radio
            }, session.Channel);
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

            //return isRadio
            //    ? await _ttsManager.ConvertTextToSpeechRadio(speaker, textSanitized)
            //    : await _ttsManager.ConvertTextToSpeech(speaker, textSanitized, isRadio: false);
        }
        catch (Exception e)
        {
            // Catch TTS exceptions to prevent a server crash.
            _sawmill.Error($"TTS System error: {e.Message}");
        }

        return default;
    }
}

public sealed class TransformSpeakerVoiceEvent : EntityEventArgs
{
    public EntityUid Sender;
    public string VoiceId;

    public TransformSpeakerVoiceEvent(EntityUid sender, string voiceId)
    {
        Sender = sender;
        VoiceId = voiceId;
    }
}
