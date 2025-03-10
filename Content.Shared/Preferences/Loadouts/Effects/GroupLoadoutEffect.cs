using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Uses a <see cref="LoadoutEffectGroupPrototype"/> prototype as a singular effect that can be re-used.
/// </summary>
public sealed partial class GroupLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public ProtoId<LoadoutEffectGroupPrototype> Proto;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var effectsProto = collection.Resolve<IPrototypeManager>().Index(Proto);

        var reasons = new List<string>();
        foreach (var effect in effectsProto.Effects)
        {
            if (effect.Validate(profile, loadout, session, collection, out reason))
                continue;

            reasons.Add(reason.ToMarkup());
        }

        // SS220 loadout sponsor tier override start

        var sponsorsReasons = new List<string>();

        if (effectsProto.SponsorTierLoadoutEffects is not null)
        {
            foreach (var effect in effectsProto.SponsorTierLoadoutEffects)
            {
                // Спонсорские подписки являются переопределяющими, поэтому наличие хотя бы одного уровня поддержки разрешает использовать вещь.
                if (effect.Validate(profile, loadout, session, collection, out var sponsorReason))
                {
                    reason = null;
                    return true;
                }

                sponsorsReasons.Add(sponsorReason.ToMarkup());
            }
        }

        reason = GetReasonFromReasonLists(reasons, sponsorsReasons);
        // SS220 loadout sponsor tier override end
        return reason == null;
    }

    private static FormattedMessage? GetReasonFromReasonLists(List<string> reasons, List<string> sponsorsReasons)
    {
        if (reasons.Count == 0)
        {
            return null;
        }

        if (sponsorsReasons.Count == 0)
        {
            return FormattedMessage.FromMarkupOrThrow(string.Join('\n', reasons));
        }

        return FormattedMessage.FromMarkupOrThrow($"{string.Join('\n', reasons)}\n{Loc.GetString("group-requirement-or")}\n{string.Join($"\n{Loc.GetString("group-requirement-or")}\n", sponsorsReasons)}");
    }
}
