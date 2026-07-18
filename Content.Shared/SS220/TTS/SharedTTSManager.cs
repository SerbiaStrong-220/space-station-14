using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.TTS;

public abstract partial class SharedTTSManager
{
    [Dependency] private IConfigurationManager _cfg = default!;

    public static readonly TTSVoicePreferences DefaultVoicePreferences = new()
    {
        new(TTSProvider.Silero, ""),
        new(TTSProvider.NTTS, "father_grigori")
    };

    private readonly HashSet<TTSProvider> _enabledProviders = [];

    public virtual void Initialize()
    {
        _cfg.OnValueChanged(CCVars220.NTTSEnabled, v => UpdateProviderEnabled(TTSProvider.NTTS, v), true);
        _cfg.OnValueChanged(CCVars220.TTSSileroEnabled, v => UpdateProviderEnabled(TTSProvider.Silero, v), true);
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

[Serializable, NetSerializable]
public sealed class TTSVoicePreferences : IEnumerable<KeyValuePair<TTSProvider, ProtoId<TTSVoicePrototype>>
{
    private readonly List<TTSProvider> _providersOrder = new();
    private readonly Dictionary<TTSProvider, ProtoId<TTSVoicePrototype>> _providersDict = new();

    public void Add(KeyValuePair<TTSProvider, ProtoId<TTSVoicePrototype> pair)
    {

    }
}
