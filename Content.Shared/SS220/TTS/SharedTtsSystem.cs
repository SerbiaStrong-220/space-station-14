using Content.Shared.Inventory;
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

    protected bool TtsEnabled = false;

    private readonly HashSet<TtsProvider> _enabledProviders = [];

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
        _cfg.OnValueChanged(CCVars220.TtsEnabled, v => TtsEnabled = v, true);
        _cfg.OnValueChanged(CCVars220.TtsNTTSEnabled, v => UpdateProviderEnabled(TtsProvider.NTTS, v), true);
        _cfg.OnValueChanged(CCVars220.TtsSileroEnabled, v => UpdateProviderEnabled(TtsProvider.Silero, v), true);

        InitializeVoiceCaches();
    }

    /// <summary>
    /// Tries to get voice preferences for the specified <paramref name="entity"/>.
    /// </summary>
    /// <param name="allowOverride">Whether to allow overriding the entity's voice preferences via <see cref="GetTtsVoiceOverrideEvent"/>.</param>
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

    /// <summary>
    /// Tries to get the currently available voice for the specified <paramref name="entity"/>, excluding those whose provider is disabled.
    /// </summary>
    /// <param name="allowOverride">Whether to allow overriding the entity's voice preferences via <see cref="GetTtsVoiceOverrideEvent"/>.</param>
    public bool TryGetAvailableVoice(Entity<TtsComponent?> entity, [NotNullWhen(true)] out TtsVoicePrototype? voice, bool allowOverride = true)
    {
        voice = null;
        return TryGetAvailableVoiceId(entity, out var id, allowOverride) && _proto.TryIndex(id, out voice);
    }

    /// <summary>
    /// Tries to get the currently available voice from the <paramref name="preferences"/>, excluding those whose provider is disabled.
    /// </summary>
    public bool TryGetAvailableVoice(TtsVoicePreferences preferences, [NotNullWhen(true)] out TtsVoicePrototype? voice)
    {
        voice = null;
        return TryGetAvailableVoiceId(preferences, out var voiceId) && _proto.TryIndex(voiceId, out voice);
    }

    /// <inheritdoc cref="TryGetAvailableVoice(Entity{TtsComponent?}, out TtsVoicePrototype?, bool)"/>
    public bool TryGetAvailableVoiceId(Entity<TtsComponent?> entity, [NotNullWhen(true)] out ProtoId<TtsVoicePrototype>? voiceId, bool allowOverride = true)
    {
        voiceId = null;
        if (!IsAnyProviderEnabled())
            return false;

        if (!TryGetVoicePreferences(entity, out var preferences, allowOverride))
            return false;

        foreach (var pair in preferences)
        {
            if (IsProviderEnabled(pair.Key))
            {
                voiceId = pair.Value;
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc cref="TryGetAvailableVoice(TtsVoicePreferences, out TtsVoicePrototype?)"/>
    public bool TryGetAvailableVoiceId(TtsVoicePreferences preferences, [NotNullWhen(true)] out ProtoId<TtsVoicePrototype>? voiceId)
    {
        foreach (var pair in preferences)
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

/// <summary>
/// Event used to collect TTS voice preference overrides for an entity (e.g., from a voice mask).
/// </summary>
public sealed class GetTtsVoiceOverrideEvent() : EntityEventArgs, IInventoryRelayEvent
{
    public readonly TtsVoicePreferences Overrides = [];

    public SlotFlags TargetSlots => SlotFlags.MASK;

    public void Add(TtsVoicePreferences other, bool force = false)
    {
        Overrides.MergeWith(other, hard: force);
    }

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
