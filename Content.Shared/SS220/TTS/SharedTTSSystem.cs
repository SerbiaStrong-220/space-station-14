using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.TTS;

public abstract partial class SharedTTSSystem : EntitySystem
{
    [Dependency] private SharedTTSManager _tts = default!;

    public bool TryGetVoiceId(EntityUid uid, [NotNullWhen(true)] out ProtoId<TTSVoicePrototype>? protoId)
    {
        protoId = null;
        if (_tts.IsAnyProviderEnabled())
            return false;

        var ev = new GetTTSVoiceOverrideEvent();
        RaiseLocalEvent(uid, ev);

        if (TryGetPreferredVoiceId(ev.Overrides, out protoId))
            return true;

        if (!TryComp<TTSComponent>(uid, out var ttsComp))
            return false;

        if (TryGetPreferredVoiceId(ttsComp.PreferredVoice, out protoId))
            return true;

        return false;
    }

    private bool TryGetPreferredVoiceId(IEnumerable<KeyValuePair<TTSProvider, ProtoId<TTSVoicePrototype>>> voices, [NotNullWhen(true)] out ProtoId<TTSVoicePrototype>? protoId)
    {
        foreach (var pair in voices)
        {
            if (_tts.IsProviderEnabled(pair.Key))
            {
                protoId = pair.Value;
                return true;
            }
        }

        protoId = null;
        return false;
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
