// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.DragonOrders;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.SS220.DragonOrders;

public sealed partial class DragonOrdersSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    /// <summary>
    /// Blackboard key with the dragon a brood member obeys. DragonCarpCompound checks it to switch to DragonBroodCompound.
    /// </summary>
    public const string BroodDragonKey = "BroodDragon";

    /// <summary>
    /// Blackboard key with the Destroy order target, read by the DragonBroodDestroyTarget utility query.
    /// </summary>
    public const string DestroyTargetKey = "DestroyTarget";

    /// <summary>
    /// Blackboard key with whoever last hurt the guarded entity, read by the DragonBroodAggressors utility query.
    /// </summary>
    public const string GuardAggressorKey = "GuardAggressor";

    /// <summary>
    /// Blackboard key with whoever last hurt this brood member, read by the DragonBroodAggressors utility query.
    /// </summary>
    public const string SelfAggressorKey = "SelfAggressor";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonOrdersComponent, MapInitEvent>(OnDragonMapInit);
        SubscribeLocalEvent<DragonOrdersComponent, ComponentShutdown>(OnDragonShutdown);
        SubscribeLocalEvent<DragonOrdersComponent, DragonOrderActionEvent>(OnOrderAction);
        SubscribeLocalEvent<DragonBroodComponent, MapInitEvent>(OnBroodMapInit);
        SubscribeLocalEvent<DragonBroodComponent, DamageChangedEvent>(OnBroodDamaged);
        SubscribeLocalEvent<DragonGuardedComponent, DamageChangedEvent>(OnGuardedDamaged);
        SubscribeLocalEvent<DragonGuardedComponent, EntityTerminatingEvent>(OnGuardedTerminating);
    }

    private void OnDragonMapInit(Entity<DragonOrdersComponent> dragon, ref MapInitEvent args)
    {
        foreach (var action in dragon.Comp.Actions)
        {
            _actions.AddAction(dragon, action);
        }

        EnsureComp<DragonGuardedComponent>(dragon).Dragons.Add(dragon);
    }

    private void OnDragonShutdown(Entity<DragonOrdersComponent> dragon, ref ComponentShutdown args)
    {
        StopGuarding(dragon, dragon.Comp.GuardTarget ?? dragon.Owner);

        var query = EntityQueryEnumerator<DragonBroodComponent, HTNComponent>();
        while (query.MoveNext(out var brood, out var htn))
        {
            if (brood.Dragon != dragon.Owner)
                continue;

            brood.Dragon = null;

            StopPlan(htn);
            htn.Blackboard.Remove<EntityUid>(BroodDragonKey);
            htn.Blackboard.Remove<EntityUid>(DestroyTargetKey);
            htn.Blackboard.Remove<EntityUid>(GuardAggressorKey);
            _htn.Replan(htn);
        }
    }

    private void OnOrderAction(Entity<DragonOrdersComponent> dragon, ref DragonOrderActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (!_examine.InRangeUnOccluded(dragon, target))
        {
            _popup.PopupEntity(Loc.GetString("dragon-order-not-visible-popup"), dragon, dragon);
            return;
        }

        if (args.Type == DragonOrderType.Destroy && (target == dragon.Owner || _faction.IsEntityFriendly(dragon.Owner, target)))
        {
            _popup.PopupEntity(Loc.GetString("dragon-order-destroy-friendly-popup"), dragon, dragon);
            return;
        }

        args.Handled = true;

        if (args.Type == DragonOrderType.Destroy)
        {
            dragon.Comp.DestroyTarget = target;
        }
        else
        {
            dragon.Comp.DestroyTarget = null;
            SetGuardTarget(dragon, target);
        }

        var targetName = Identity.Entity(target, EntityManager);
        string dragonPopup;
        string broodPopup;

        if (args.Type == DragonOrderType.Destroy)
        {
            dragonPopup = Loc.GetString("dragon-order-destroy-popup", ("target", targetName));
            broodPopup = Loc.GetString("dragon-order-brood-destroy-popup", ("target", targetName));
        }
        else if (target == dragon.Owner)
        {
            dragonPopup = Loc.GetString("dragon-order-guard-self-popup");
            broodPopup = Loc.GetString("dragon-order-brood-guard-dragon-popup");
        }
        else
        {
            dragonPopup = Loc.GetString("dragon-order-guard-popup", ("target", targetName));
            broodPopup = Loc.GetString("dragon-order-brood-guard-popup", ("target", targetName));
        }

        if (CommandBrood(dragon, broodPopup) == 0)
        {
            _popup.PopupEntity(Loc.GetString("dragon-order-no-brood-popup"), dragon, dragon);
            return;
        }

        _popup.PopupEntity(dragonPopup, dragon, dragon);
    }

    private void OnBroodMapInit(Entity<DragonBroodComponent> member, ref MapInitEvent args)
    {
        if (member.Comp.Dragon != null)
            return;

        var memberCoords = _transform.GetMapCoordinates(member);
        EntityUid? nearest = null;
        var nearestDistanceSquared = float.MaxValue;

        var query = EntityQueryEnumerator<DragonOrdersComponent, TransformComponent>();
        while (query.MoveNext(out var dragon, out var orders, out var xform))
        {
            var dragonCoords = _transform.GetMapCoordinates(dragon, xform);
            if (dragonCoords.MapId != memberCoords.MapId || _mobState.IsDead(dragon))
                continue;

            var distanceSquared = (dragonCoords.Position - memberCoords.Position).LengthSquared();
            if (distanceSquared > orders.AdoptRange * orders.AdoptRange || distanceSquared >= nearestDistanceSquared)
                continue;

            nearest = dragon;
            nearestDistanceSquared = distanceSquared;
        }

        if (nearest != null)
            AddToBrood(nearest.Value, member);
    }

    private void OnBroodDamaged(Entity<DragonBroodComponent> member, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.Origin is not { } aggressor ||
            aggressor == member.Owner ||
            _faction.IsEntityFriendly(member.Owner, aggressor) ||
            !TryComp<HTNComponent>(member, out var htn))
        {
            return;
        }

        htn.Blackboard.SetValue(SelfAggressorKey, aggressor);
    }

    private void OnGuardedDamaged(Entity<DragonGuardedComponent> guarded, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin is not { } aggressor || aggressor == guarded.Owner)
            return;

        foreach (var dragon in guarded.Comp.Dragons)
        {
            if (aggressor == dragon || _faction.IsEntityFriendly(dragon, aggressor))
                continue;

            var query = EntityQueryEnumerator<DragonBroodComponent, HTNComponent>();
            while (query.MoveNext(out var brood, out var htn))
            {
                if (brood.Dragon == dragon)
                    htn.Blackboard.SetValue(GuardAggressorKey, aggressor);
            }
        }
    }

    private void OnGuardedTerminating(Entity<DragonGuardedComponent> guarded, ref EntityTerminatingEvent args)
    {
        foreach (var dragon in guarded.Comp.Dragons)
        {
            if (dragon == guarded.Owner ||
                TerminatingOrDeleted(dragon) ||
                !TryComp<DragonOrdersComponent>(dragon, out var orders) ||
                orders.GuardTarget != guarded.Owner)
            {
                continue;
            }

            orders.GuardTarget = null;
            EnsureComp<DragonGuardedComponent>(dragon).Dragons.Add(dragon);
            CommandBrood((dragon, orders), Loc.GetString("dragon-order-brood-guard-lost-popup"));
            _popup.PopupEntity(Loc.GetString("dragon-order-guard-lost-popup"), dragon, dragon);
        }
    }

    /// <summary>
    /// Makes the entity a member of the dragon's brood, following the dragon's current orders.
    /// Does nothing if the dragon can't give orders or the entity has no HTN.
    /// </summary>
    public void AddToBrood(EntityUid dragon, EntityUid member)
    {
        if (!TryComp<DragonOrdersComponent>(dragon, out var orders) || !TryComp<HTNComponent>(member, out var htn))
            return;

        EnsureComp<DragonBroodComponent>(member).Dragon = dragon;
        ApplyOrders((member, htn), (dragon, orders));
    }

    private void SetGuardTarget(Entity<DragonOrdersComponent> dragon, EntityUid target)
    {
        var current = dragon.Comp.GuardTarget ?? dragon.Owner;
        if (current == target)
            return;

        StopGuarding(dragon, current);
        dragon.Comp.GuardTarget = target == dragon.Owner ? null : target;
        EnsureComp<DragonGuardedComponent>(target).Dragons.Add(dragon);
    }

    private void StopGuarding(EntityUid dragon, EntityUid guarded)
    {
        if (TerminatingOrDeleted(guarded) || !TryComp<DragonGuardedComponent>(guarded, out var comp))
            return;

        comp.Dragons.Remove(dragon);
        if (comp.Dragons.Count == 0)
            RemComp(guarded, comp);
    }

    private bool HasLivingDragon(DragonBroodComponent brood)
    {
        return brood.Dragon is { } dragon && !TerminatingOrDeleted(dragon) && !_mobState.IsDead(dragon);
    }

    /// <summary>
    /// Applies the dragon's current orders to its brood, adopting nearby brood without a living dragon.
    /// The <paramref name="broodPopup"/> is shown to player-controlled members, since orders only drive the NPC.
    /// </summary>
    /// <returns>How many living brood members got the orders.</returns>
    private int CommandBrood(Entity<DragonOrdersComponent> dragon, string broodPopup)
    {
        var dragonCoords = _transform.GetMapCoordinates(dragon);
        var count = 0;

        var query = EntityQueryEnumerator<DragonBroodComponent, HTNComponent, TransformComponent>();
        while (query.MoveNext(out var member, out var brood, out var htn, out var xform))
        {
            if (brood.Dragon != dragon.Owner)
            {
                if (HasLivingDragon(brood) ||
                    !_transform.GetMapCoordinates(member, xform).InRange(dragonCoords, dragon.Comp.AdoptRange))
                {
                    continue;
                }

                brood.Dragon = dragon.Owner;
            }

            ApplyOrders((member, htn), dragon);

            if (_mobState.IsDead(member))
                continue;

            count++;

            if (HasComp<ActorComponent>(member))
                _popup.PopupEntity(broodPopup, member, member, PopupType.Medium);
        }

        return count;
    }

    private void StopPlan(HTNComponent htn)
    {
        if (htn.Plan == null)
            return;

        _htn.ShutdownTask(htn.Plan.CurrentOperator, htn.Blackboard, HTNOperatorStatus.Failed);
        _htn.ShutdownPlan(htn);
    }

    private void ApplyOrders(Entity<HTNComponent> member, Entity<DragonOrdersComponent> dragon)
    {
        StopPlan(member.Comp);

        var blackboard = member.Comp.Blackboard;
        blackboard.SetValue(BroodDragonKey, dragon.Owner);
        blackboard.SetValue(NPCBlackboard.FollowTarget, new EntityCoordinates(dragon.Comp.GuardTarget ?? dragon.Owner, Vector2.Zero));
        blackboard.Remove<EntityUid>(GuardAggressorKey);

        if (dragon.Comp.DestroyTarget is { } target && !TerminatingOrDeleted(target))
            blackboard.SetValue(DestroyTargetKey, target);
        else
            blackboard.Remove<EntityUid>(DestroyTargetKey);

        _htn.Replan(member.Comp);
    }
}
