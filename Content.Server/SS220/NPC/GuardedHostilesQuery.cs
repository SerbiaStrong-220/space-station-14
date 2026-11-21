// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.NPC;
using Content.Server.NPC.Queries.Queries;

namespace Content.Server.SS220.NPC;

/// <summary>
/// Returns nearby hostiles that are close to <see cref="NPCBlackboard.FollowTarget"/>, except the followed entity itself.
/// </summary>
public sealed partial class GuardedHostilesQuery : UtilityQuery
{
    /// <summary>
    /// Blackboard key with the guard radius around the follow target. Vision radius is used if it is missing.
    /// </summary>
    [DataField]
    public string RangeKey = "GuardRange";
}
