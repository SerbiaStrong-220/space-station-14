// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.SS220.Loadouts;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.Loadouts;

public sealed partial class SponsorTierRequirementLoadoutEffect : SharedSponsorTierRequirementLoadoutEffect
{
    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        var contentData = session?.ContentData();
        if (contentData == null)
        {
            reason = FormattedMessage.Empty;
            return false;
        }

        // The premium-checker service was unavailable/errored when we tried to fetch this player's
        // sponsor status, so we can't tell "not a sponsor" from "couldn't ask". Fail open rather than
        // silently stripping an already-selected sponsor item on every restart/reconnect.
        if (contentData.SponsorInfoFetchFailed)
            return true;

        if (contentData.SponsorInfo?.Tiers is not { } playerTiers)
        {
            reason = FormattedMessage.Empty;
            return false;
        }

        var prototypeManager = collection.Resolve<IPrototypeManager>();
        var result = CheckRequirement(playerTiers);

        if (!result)
            reason = FormInsufficientReason(prototypeManager);

        return result;
    }
}
