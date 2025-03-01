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
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null)
            return;

        if (TryGetVoiceMaskUid(uid, out var maskUid))
        {
            var voiceEv = new TransformSpeakerVoiceEvent(maskUid.Value, voiceId);
            RaiseLocalEvent(maskUid.Value, voiceEv);
            voiceId = voiceEv.VoiceId;
        }

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice.Speaker, args.IsRadio);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker)
    {
        using var ttsResponse = await GenerateTts(message, speaker, TtsKind.Default);
        if (!ttsResponse.TryGetValue(out var audioData)) return;
        var playTtsMessage = new MsgPlayTts
        {
            Data = audioData,
            SourceUid = GetNetEntity(uid),
        };
        foreach (var receiver in Filter.Pvs(uid).Recipients)
        {
            _netManager.ServerSendMessage(playTtsMessage, receiver.Channel);
        }
    }

    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, string speaker, bool isRadio)
    {
        // If it's a whisper into a radio, generate speech without whisper
        // attributes to prevent an additional speech synthesis event
        using var ttsResponse = await GenerateTts(message, speaker, TtsKind.Whisper);
        if (!ttsResponse.TryGetValue(out var audioData))
            return;

        using var obfTtsResponse = await GenerateTts(obfMessage, speaker, TtsKind.Whisper);
        if (!obfTtsResponse.TryGetValue(out var obfAudioData))
            return;

        // TODO: Check obstacles
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;

        var fullTtsMessage = new MsgPlayTts
        {
            Data = audioData,
            SourceUid = GetNetEntity(uid),
            Kind = TtsKind.Whisper,
        };

        var obfuscatedTtsMessage = new MsgPlayTts
        {
            Data = obfAudioData,
            SourceUid = GetNetEntity(uid),
            Kind = TtsKind.Whisper,
        };

        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

            if (distance > ChatSystem.WhisperMuffledRange)
                continue;

            var netMessageToSend = distance > ChatSystem.WhisperClearRange ? obfuscatedTtsMessage : fullTtsMessage;

            _netManager.ServerSendMessage(netMessageToSend, session.Channel);
        }
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
