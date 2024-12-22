// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.SS220.Discord;
using Content.Shared.SS220.Shlepovend;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Loadouts;

/// <summary>
/// Checks for a SponsorTier requirement to be met.
/// </summary>
public abstract partial class SharedSponsorTierRequirementLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public SponsorTier Requirement = default!;

    protected bool CheckRequirement(SponsorTier[] playerTiers)
    {
        var result = false;
        foreach (var tier in playerTiers)
        {
            if ((int)tier < (int)Requirement)
                continue;

            result = true;
            break;
        }

        return result;
    }

    protected FormattedMessage FormInsufficientReason(IPrototypeManager prototypeManager)
    {
        // Such a hack
        var rewardGroups = prototypeManager.GetInstances<ShlepaRewardGroupPrototype>();
        var tierName = "неизвестно"; // I will not make localisation for RewardGroups

        foreach (var (_, group) in rewardGroups)
        {
            if (group.RequiredRole == null || (SponsorTier)group.RequiredRole != Requirement)
                continue;

            tierName = group.Name;
            break;
        }

        return FormattedMessage.FromMarkupPermissive(Loc.GetString("sponsor-tier-insufficient", ("tier", tierName)));
    }
}
