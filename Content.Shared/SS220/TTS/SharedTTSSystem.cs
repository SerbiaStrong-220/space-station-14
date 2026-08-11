using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.TTS;

public abstract partial class SharedTtsSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public const string TtsCommandsPrefix = "tts.";

    public event Action<TtsProvider, bool>? OnTtsProviderStateChanged;

    private readonly HashSet<TtsProvider> _enabledProviders = [];

    protected bool TtsEnabled = false;

    public static readonly TtsVoicePreferences DefaultVoicePreferences = new()
    {
        [TtsProvider.Silero] = "SileroTest",
        [TtsProvider.NTTS] = "father_grigori"
    };

    public static readonly TtsVoicePreferences DefaultAnnouncementVoicePreferences = new()
    {
        [TtsProvider.Silero] = "SileroDefault",
        [TtsProvider.NTTS] = "glados"
    };

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCVars220.TTSEnabled, v => TtsEnabled = v, true);
        _cfg.OnValueChanged(CCVars220.TtsNTTSEnabled, v => UpdateProviderEnabled(TtsProvider.NTTS, v), true);
        _cfg.OnValueChanged(CCVars220.TtsSileroEnabled, v => UpdateProviderEnabled(TtsProvider.Silero, v), true);

        InitializeVoiceCaches();
    }

    public bool TryGetVoicePreferences(Entity<TtsComponent?> entity, [NotNullWhen(true)] out TtsVoicePreferences? pref, bool allowOverride = true)
    {
        pref = null;
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return false;

        pref = entity.Comp.VoicePreferences.Clone();
        if (allowOverride)
        {
            var ev = new GetTtsVoiceOverrideEvent();
            RaiseLocalEvent(entity, ev);

            pref.HardMergeWith(ev.Overrides);
        }

        return true;
    }

    public bool TryGetAvailableVoice(Entity<TtsComponent?> entity, [NotNullWhen(true)] out TtsVoicePrototype? voice, bool allowOverride = true)
    {
        voice = null;
        return TryGetAvailableVoiceId(entity, out var id, allowOverride) && _proto.TryIndex(id, out voice);
    }

    public bool TryGetAvailableVoiceId(Entity<TtsComponent?> entity, [NotNullWhen(true)] out ProtoId<TtsVoicePrototype>? voiceId, bool allowOverride = true)
    {
        voiceId = null;
        if (!IsAnyProviderEnabled())
            return false;

        if (!TryGetVoicePreferences(entity, out var preferences))
            return false;

        return TryGetPreferredVoiceId(preferences, out voiceId);
    }

    protected bool TryGetPreferredVoice(IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> voices, [NotNullWhen(true)] out TtsVoicePrototype? voice)
    {
        voice = null;
        return TryGetPreferredVoiceId(voices, out var id) && _proto.TryIndex(id, out voice);
    }

    protected bool TryGetPreferredVoiceId(IEnumerable<KeyValuePair<TtsProvider, ProtoId<TtsVoicePrototype>>> voices, [NotNullWhen(true)] out ProtoId<TtsVoicePrototype>? voiceId)
    {
        foreach (var pair in voices)
        {
            if (IsProviderEnabled(pair.Key))
            {
                voiceId = pair.Value;
                return true;
            }
        }

        voiceId = null;
        return false;
    }

    public bool IsProviderEnabled(TtsProvider provider)
    {
        return TtsEnabled && _enabledProviders.Contains(provider);
    }

    public bool IsAnyProviderEnabled()
    {
        return TtsEnabled && _enabledProviders.Count > 0;
    }

    public bool TryGetDefaultPreferredVoice([NotNullWhen(true)] out ProtoId<TtsVoicePrototype>? protoId)
    {
        protoId = null;
        if (!IsAnyProviderEnabled())
            return false;

        foreach (var (provider, id) in DefaultVoicePreferences)
        {
            if (!IsProviderEnabled(provider))
                continue;

            protoId = id;
            break;
        }

        return protoId != null;
    }

    private void UpdateProviderEnabled(TtsProvider provider, bool enabled)
    {
        bool changed;
        if (enabled)
            changed = _enabledProviders.Add(provider);
        else
            changed = _enabledProviders.Remove(provider);

        if (changed)
            OnTtsProviderStateChanged?.Invoke(provider, enabled);
    }
}

public sealed class GetTtsVoiceOverrideEvent() : EntityEventArgs
{
    public readonly TtsVoicePreferences Overrides = [];

    public void Add(TtsProvider provider, ProtoId<TtsVoicePrototype> protoId, bool force = false)
    {
        if (Overrides.ContainsKey(provider) && !force)
            return;

        Overrides[provider] = protoId;
    }
}

[Serializable, NetSerializable]
public enum TtsProvider : byte
{
    NTTS,
    Silero
}

[Serializable, NetSerializable]
public enum TtsKind : byte
{
    Say,
    Radio,
    Whisper,
    Announce,
    Telepathy,
    VoiceTest
}

[Serializable, NetSerializable]
public struct TtsMetadata
{
    public required TtsKind Kind;

    public TtsProvider? Provider;
    public string? ChannelPrototype;
    public NetEntity? Source;
    public NetEntity? PlayEntity;
}

[Flags]
public enum AudioWithTTSPlayOperation : byte
{
    NotPlay = 1 << 0,
    PlayAudio = 1 << 1,
    PlayTTS = 1 << 2,

    PlayAll = PlayAudio | PlayTTS,
}
