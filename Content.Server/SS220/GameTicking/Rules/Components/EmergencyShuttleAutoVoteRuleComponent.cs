// © SS220, MIT, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.txt

namespace Content.Server.SS220.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class EmergencyShuttleAutoVoteRuleComponent : Component
{
    /// <summary>
    /// When lats vote was made
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastEvacVoteTime = TimeSpan.Zero;

    /// <summary>
    /// Time after round start when we want to make first vote for round end
    /// </summary>
    [DataField]
    public TimeSpan VoteStartTime = TimeSpan.FromMinutes(80f);

    /// <summary>
    /// How much time we wait before next vote
    /// </summary>
    [DataField]
    public TimeSpan IntervalBetweenVotes = TimeSpan.FromMinutes(30f);

    /// <summary>
    /// Round duration after which we force evac call
    /// </summary>
    [DataField]
    public TimeSpan? ForceEvacTime = null;
}
