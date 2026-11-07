// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.SS220.NPC.CableGnaw;

namespace Content.Server.SS220.NPC.HTN.PrimitiveTasks.Operators.Specific;

/// <summary>
/// Gnaws the target cable. The gnawer dies of electrocution.
/// </summary>
public sealed partial class GnawCableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private CableGnawSystem _gnaw = default!;

    [DataField]
    public string TargetKey = "Target";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gnaw = sysManager.GetEntitySystem<CableGnawSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager) ||
            !blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) ||
            _entManager.Deleted(target))
        {
            return HTNOperatorStatus.Failed;
        }

        return _gnaw.TryGnaw(owner, target)
            ? HTNOperatorStatus.Finished
            : HTNOperatorStatus.Failed;
    }
}
