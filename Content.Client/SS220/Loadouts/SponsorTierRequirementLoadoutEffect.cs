// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.Discord;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.SS220.Loadouts;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.SS220.Loadouts;

public sealed partial class SponsorTierRequirementLoadoutEffect : SharedSponsorTierRequirementLoadoutEffect
{
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
        var result = CheckRequirement(playerTiers);

        if (!result)
            reason = FormInsufficientReason(prototypeManager);

        return result;
    }
}
