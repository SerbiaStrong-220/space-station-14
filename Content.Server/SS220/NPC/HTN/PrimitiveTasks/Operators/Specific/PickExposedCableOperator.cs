// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Systems;
using Content.Server.SS220.NPC.CableGnaw;

namespace Content.Server.SS220.NPC.HTN.PrimitiveTasks.Operators.Specific;

/// <summary>
/// Rarely picks a nearby exposed cable to gnaw, but only if no food is visible.
/// </summary>
public sealed partial class PickExposedCableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private NPCUtilitySystem _utility = default!;
    private CableGnawSystem _gnaw = default!;

    [DataField]
    public string CableQuery = "NearbyExposedCables";

    [DataField]
    public string FoodQuery = "NearbyFood";

    /// <summary>
    /// Chance to actually go for a cable once the cooldown has passed.
    /// </summary>
    [DataField]
    public float Chance = 0.15f;

    [DataField]
    public TimeSpan MinCooldown = TimeSpan.FromMinutes(2);

    [DataField]
    public TimeSpan MaxCooldown = TimeSpan.FromMinutes(5);

    [DataField]
    public string TargetKey = "Target";

    [DataField]
    public string TargetCoordinatesKey = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _utility = sysManager.GetEntitySystem<NPCUtilitySystem>();
        _gnaw = sysManager.GetEntitySystem<CableGnawSystem>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
            return (false, null);

        if (!_gnaw.IsOffCooldown(owner, MinCooldown, MaxCooldown))
            return (false, null);

        // Food nearby - mice prefer it, no cable snacks.
        if (_utility.GetEntities(blackboard, FoodQuery, bestOnly: false).Entities.Count > 0)
            return (false, null);

        if (!_gnaw.RollAttempt(owner, Chance, MinCooldown, MaxCooldown))
            return (false, null);

        var cables = _utility.GetEntities(blackboard, CableQuery, bestOnly: false);

        foreach (var (cable, _) in cables.Entities.OrderByDescending(x => x.Value))
        {
            if (!_gnaw.IsCableExposed(cable))
                continue;

            var coords = _entManager.GetComponent<TransformComponent>(cable).Coordinates;

            return (true, new Dictionary<string, object>
            {
                { TargetKey, cable },
                { TargetCoordinatesKey, coords },
            });
        }

        return (false, null);
    }
}
