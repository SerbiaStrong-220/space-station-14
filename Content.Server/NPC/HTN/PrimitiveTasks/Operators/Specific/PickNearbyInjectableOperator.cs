using System.Threading;
using System.Threading.Tasks;
using Content.Shared.NPC.Components;
using Content.Server.NPC.Pathfinding;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Bots;
using Content.Shared.Emag.Components;
using Content.Shared.Stealth.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes; //ss220 fix medibot

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class PickNearbyInjectableOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!; //ss220 fix medibot

    private EntityLookupSystem _lookup = default!;
    private MedibotSystem _medibot = default!;
    private PathfindingSystem _pathfinding = default!;
    private SharedContainerSystem _container = default!; //ss220 stealth inject fix

    [DataField("rangeKey")] public string RangeKey = NPCBlackboard.MedibotInjectRange;

    /// <summary>
    /// Target entity to inject
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField("targetMoveKey", required: true)]
    public string TargetMoveKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _medibot = sysManager.GetEntitySystem<MedibotSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _container = sysManager.GetEntitySystem<SharedContainerSystem>(); //ss220 npc stealth inject fix
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<float>(RangeKey, out var range, _entManager))
            return (false, null);

        if (!_entManager.TryGetComponent<MedibotComponent>(owner, out var medibot))
            return (false, null);

        var damageQuery = _entManager.GetEntityQuery<DamageableComponent>();
        var injectQuery = _entManager.GetEntityQuery<InjectableSolutionComponent>();
        var recentlyInjected = _entManager.GetEntityQuery<NPCRecentlyInjectedComponent>();
        var mobState = _entManager.GetEntityQuery<MobStateComponent>();
        //var emaggedQuery = _entManager.GetEntityQuery<EmaggedComponent>(); //ss220 fix medibot
        var stealthQuery = _entManager.GetEntityQuery<StealthComponent>(); //ss220 medibot inject in stealth entity fix

        foreach (var entity in _lookup.GetEntitiesInRange(owner, range))
        {
            if (mobState.TryGetComponent(entity, out var state) &&
                injectQuery.HasComponent(entity) &&
                damageQuery.TryGetComponent(entity, out var damage) &&
                !recentlyInjected.HasComponent(entity))
            {
                //ss220 medibot inject in stealth entity fix start
                if (_container.IsEntityInContainer(entity))
                {
                    continue;
                }
                if (stealthQuery.TryGetComponent(entity, out var stealthComponent) && stealthComponent.Enabled)
                {
                    continue;
                }
                //ss220 medibot inject in stealth entity fix end

                // no treating dead bodies
                if (!_medibot.TryGetTreatment(medibot, state.CurrentState, out var treatment))
                    continue;

                //ss220 fix medibot start
                var emagged = _entManager.HasComponent<EmaggedComponent>(owner);

                if (!treatment.IsValid(damage.Damage, emagged, _proto))
                    continue;
                //ss220 fix medibot end

                //Needed to make sure it doesn't sometimes stop right outside it's interaction range
                var pathRange = SharedInteractionSystem.InteractionRange - 1f;
                var path = await _pathfinding.GetPath(owner, entity, pathRange, cancelToken);

                if (path.Result == PathResult.NoPath)
                    continue;

                return (true, new Dictionary<string, object>()
                {
                    {TargetKey, entity},
                    {TargetMoveKey, _entManager.GetComponent<TransformComponent>(entity).Coordinates},
                    {NPCBlackboard.PathfindKey, path},
                });
            }
        }

        return (false, null);
    }
}
