// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.NPC;
using Content.Server.NPC.Queries.Queries;

namespace Content.Server.SS220.NPC;

/// <summary>
/// Returns the entity stored in the blackboard under <see cref="Key"/>, whatever it is.
/// </summary>
public sealed partial class BlackboardTargetQuery : UtilityQuery
{
    [DataField(required: true)]
    public string Key = default!;

    /// <summary>
    /// Blackboard key with a radius around <see cref="NPCBlackboard.FollowTarget"/>.
    /// If the blackboard has it, the entity is skipped once it is farther than that.
    /// </summary>
    [DataField]
    public string? LeashRangeKey;
}
