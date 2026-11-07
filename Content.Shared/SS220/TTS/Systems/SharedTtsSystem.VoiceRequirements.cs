using Content.Shared.SS220.TTS.Prototypes;
using Content.Shared.SS220.TTS.Requirements;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.TTS.Systems;

public partial class SharedTtsSystem
{
    public bool IsPassVoiceRequirements(TtsVoicePrototype proto, TtsVoiceRequirementCheckData data)
    {
        return IsPassVoiceRequirements(proto, data, out _);
    }

    public bool IsPassVoiceRequirements(TtsVoicePrototype proto, TtsVoiceRequirementCheckData data, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        if (proto.Requirement == null)
            return true;

        if (!data.IsRequirementAllowed(proto.Requirement))
            return true;

        return proto.Requirement.Check(EntityManager, data, out reason);
    }

    public TtsVoicePreferences RemoveNotAvailableVoices(TtsVoicePreferences preferences, TtsVoiceRequirementCheckData data)
    {
        var result = new TtsVoicePreferences();
        foreach (var (provider, voiceId) in preferences)
        {
            if (!_proto.TryIndex(voiceId, out var voice))
                continue;

            if (!IsPassVoiceRequirements(voice, data))
                continue;

            result.Add(provider, voiceId);
        }

        return result;
    }
}
