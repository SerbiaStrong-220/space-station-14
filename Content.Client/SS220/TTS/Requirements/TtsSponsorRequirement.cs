using Content.Client.SS220.Discord;
using Content.Shared.SS220.Discord;
using Content.Shared.SS220.TTS.Requirements;
using Robust.Shared.Player;

namespace Content.Client.SS220.TTS.Requirements;

public sealed partial class TtsSponsorRequirement : SharedTtsSponsorRequirement
{
    public override SponsorTier[]? GetSponsorTiers(ICommonSession session)
    {
        return IoCManager.Resolve<DiscordPlayerInfoManager>().GetSponsorTier();
    }
}
