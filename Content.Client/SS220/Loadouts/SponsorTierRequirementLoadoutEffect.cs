// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.Discord;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.SS220.Discord;
using Content.Shared.SS220.Shlepovend;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.SS220.Loadouts;

/// <summary>
/// Checks for a SponsorTier requirement to be met.
/// </summary>
public sealed partial class SponsorTierRequirementLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public SponsorTier Requirement = default!;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        if (session == null)
        {
            reason = FormattedMessage.Empty;
            return false;
        }

        var prototypeManager = collection.Resolve<IPrototypeManager>();
        var discordManager = collection.Resolve<DiscordPlayerInfoManager>();
        var playerTiers = discordManager.GetSponsorTier();

        var result = false;
        foreach (var tier in playerTiers)
        {
            if ((int)tier < (int)Requirement)
                continue;

            result = true;
            break;
        }

        if (!result)
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

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("sponsor-tier-insufficient", ("tier", tierName)));
            return false;
        }

        return true;
    }
}
