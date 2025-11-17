// © SS220, MIT, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.txt

using Robust.Shared.GameStates;

namespace Content.Server.SS220.GameTicking.Rules.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class EmergencyShuttleAutoVoteRuleComponent : Component
{
    /// <summary>
    /// When lats vote was made
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastEvacVoteTime = TimeSpan.Zero;

    /// <summary>
    /// Time after round start when we want to make first vote for round end
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan VoteStartTime = TimeSpan.FromMinutes(80f);

    /// <summary>
    /// How much time we wait before next vote
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan IntervalBetweenVotes = TimeSpan.FromMinutes(15f);

    /// <summary>
    /// Round duration after which we force evac call
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan? ForceEvacTime = TimeSpan.FromHours(2.5f);
}
