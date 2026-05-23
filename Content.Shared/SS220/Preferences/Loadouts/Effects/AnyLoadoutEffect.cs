using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Checks a list of effects. Returns true if at least one is valid (OR logic).
/// </summary>
public sealed partial class AnyLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<LoadoutEffect> Effects = new();

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var reasons = new List<string>();

        foreach (var effect in Effects)
        {
            if (effect.Validate(profile, loadout, session, collection, out var effectReason))
            {
                reason = null;
                return true;
            }

            reasons.Add(effectReason.ToMarkup());
        }

        reason = FormattedMessage.FromMarkupOrThrow(string.Join($"\n{Loc.GetString("group-requirement-or")}\n", reasons));
        return false;
    }
}
