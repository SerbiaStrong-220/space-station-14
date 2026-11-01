using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.TTS.Requirements;

[Serializable, NetSerializable]
public sealed partial class TtsAllRequirement : TtsVoiceRequirement
{
    [DataField(required: true)]
    public List<TtsVoiceRequirement> Requirements = [];

    public override bool Check(IEntityManager entityManager, TtsVoiceRequirementCheckData data, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        if (Requirements.Count == 0)
            return true;

        var reasons = new List<string>();
        foreach (var requirement in Requirements)
        {
            if (!data.IsRequirementAllowed(requirement))
                continue;

            if (requirement.Check(entityManager, data, out reason))
                continue;

            reasons.Add(reason.ToMarkup());
        }

        if (reasons.Count == 0)
            return true;

        reason = FormattedMessage.FromMarkupPermissive(string.Join($"\n{Loc.GetString("generic-and")}\n", reasons));
        return false;
    }
}
