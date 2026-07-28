using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.TTS;

public abstract partial class SharedTTSSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly HashSet<TTSProvider> _enabledProviders = [];

    public static readonly IReadOnlyDictionary<TTSProvider, ProtoId<TTSVoicePrototype>> DefaultVoicePreferences =
        new Dictionary<TTSProvider, ProtoId<TTSVoicePrototype>>()
        {
            [TTSProvider.Silero] = "SileroDefault",
            [TTSProvider.NTTS] = "father_grigori"
        };

    public static readonly IReadOnlyDictionary<TTSProvider, ProtoId<TTSVoicePrototype>> DefaultAnnouncementVoicePreferences =
        new Dictionary<TTSProvider, ProtoId<TTSVoicePrototype>>()
        {
            [TTSProvider.Silero] = "SileroDefault",
            [TTSProvider.NTTS] = "glados"
        };

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCVars220.NTTSEnabled, v => UpdateProviderEnabled(TTSProvider.NTTS, v), true);
        _cfg.OnValueChanged(CCVars220.TTSSileroEnabled, v => UpdateProviderEnabled(TTSProvider.Silero, v), true);
    }

    public bool TryGetVoice(EntityUid uid, [NotNullWhen(true)] out TTSVoicePrototype? voice)
    {
        voice = null;
        return TryGetVoiceId(uid, out var id) && _proto.TryIndex(id, out voice);
    }

    public bool TryGetVoiceId(EntityUid uid, [NotNullWhen(true)] out ProtoId<TTSVoicePrototype>? voiceId)
    {
        voiceId = null;
        if (IsAnyProviderEnabled())
            return false;

        var ev = new GetTTSVoiceOverrideEvent();
        RaiseLocalEvent(uid, ev);

        if (TryGetPreferredVoiceId(ev.Overrides, out voiceId))
            return true;

        if (!TryComp<TTSComponent>(uid, out var ttsComp))
            return false;

        if (TryGetPreferredVoiceId(ttsComp.PreferredVoice, out voiceId))
            return true;

        return false;
    }

    protected bool TryGetPreferredVoice(IEnumerable<KeyValuePair<TTSProvider, ProtoId<TTSVoicePrototype>>> voices, [NotNullWhen(true)] out TTSVoicePrototype? voice)
    {
        voice = null;
        return TryGetPreferredVoiceId(voices, out var id) && _proto.TryIndex(id, out voice);
    }

    protected bool TryGetPreferredVoiceId(IEnumerable<KeyValuePair<TTSProvider, ProtoId<TTSVoicePrototype>>> voices, [NotNullWhen(true)] out ProtoId<TTSVoicePrototype>? voiceId)
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

    public bool IsProviderEnabled(TTSProvider provider)
    {
        return _enabledProviders.Contains(provider);
    }

    public bool IsAnyProviderEnabled()
    {
        return _enabledProviders.Count > 0;
    }

    public bool TryGetDefaultPreferredVoice([NotNullWhen(true)] out ProtoId<TTSVoicePrototype>? protoId)
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

    private void UpdateProviderEnabled(TTSProvider provider, bool enabled)
    {
        if (enabled)
            _enabledProviders.Add(provider);
        else
            _enabledProviders.Remove(provider);
    }
}

public sealed class GetTTSVoiceOverrideEvent() : EntityEventArgs
{
    private readonly Dictionary<TTSProvider, ProtoId<TTSVoicePrototype>> _overrides = new();

    public IReadOnlyDictionary<TTSProvider, ProtoId<TTSVoicePrototype>> Overrides => _overrides;

    public void Add(TTSProvider provider, ProtoId<TTSVoicePrototype> protoId, bool force = false)
    {
        if (_overrides.ContainsKey(provider) && !force)
            return;

        _overrides[provider] = protoId;
    }
}
