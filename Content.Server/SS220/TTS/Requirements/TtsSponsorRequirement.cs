using Content.Shared.Players;
using Content.Shared.SS220.Discord;
using Content.Shared.SS220.TTS.Requirements;
using Robust.Shared.Player;

namespace Content.Server.SS220.TTS.Requirements;

public sealed partial class TtsSponsorRequirement : SharedTtsSponsorRequirement
{
    public override SponsorTier[]? GetSponsorTiers(ICommonSession session)
    {
        return session.ContentData()?.SponsorInfo?.Tiers;
    }
}
