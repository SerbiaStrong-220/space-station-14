using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Unit;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.SS220.Maths;
using Content.Shared.Stunnable;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using SharedFelinidPipecrawlSystem = Content.Shared.SS220.Felinid.FelinidPipecrawlSystem;

namespace Content.Server.SS220.Felinid;

public sealed class FelinidPipecrawlSystem : EntitySystem
{
    private static readonly EntProtoId DisposalHolderPrototype = "DisposalHolder";
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedFelinidPipecrawlSystem _sharedPipecrawl = default!;
    [Dependency] private readonly SharedContentEyeSystem _contentEye = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly DisposableSystem _disposableSystem = default!;
    [Dependency] private readonly DisposalUnitSystem _disposalUnitSystem = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly DisposalTubeSystem _disposalTubeSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FelinidPipecrawlComponent, FelinidPipecrawlActionEvent>(OnPipecrawl);
        SubscribeLocalEvent<FelinidPipecrawlComponent, InhaleLocationEvent>(OnPipecrawlInhale);
        SubscribeLocalEvent<FelinidPipecrawlComponent, ExhaleLocationEvent>(OnPipecrawlExhale);
        SubscribeLocalEvent<FelinidPipecrawlComponent, AtmosExposedGetAirEvent>(OnPipecrawlGetAir);
        SubscribeLocalEvent<FelinidPipecrawlComponent, MoveInputEvent>(OnPipecrawlMoveInput);
        SubscribeLocalEvent<FelinidPipecrawlComponent, MobStateChangedEvent>(OnPipecrawlMobStateChanged);
        SubscribeLocalEvent<EntGotInsertedIntoContainerMessage>(OnEntityInsertedIntoContainer);
        SubscribeLocalEvent<FelinidPipecrawlContentsComponent, EntGotRemovedFromContainerMessage>(OnPipeContentsRemoved);
    }

    public override void Update(float frameTime)
    {
        UpdatePipecrawlers();
    }

    private void UpdatePipecrawlers()
    {
        var query = EntityQueryEnumerator<FelinidPipecrawlComponent>();
        while (query.MoveNext(out var uid, out var pipecrawl))
        {
            if (!pipecrawl.Active)
                continue;

            if (pipecrawl.CurrentTube is not { } currentTube || !Exists(currentTube))
            {
                TryForceExitPipecrawl((uid, pipecrawl), false);
                continue;
            }

            if (pipecrawl.NextTube == null && TryComp<InputMoverComponent>(uid, out var mover))
                TryStartPipecrawlMove((uid, pipecrawl), mover.HeldMoveButtons);

            if (pipecrawl.NextTube is not { } nextTube)
                continue;

            if (!Exists(nextTube))
            {
                pipecrawl.NextTube = null;
                pipecrawl.TravelDirection = Direction.Invalid;
                pipecrawl.TravelStartedAt = TimeSpan.Zero;
                pipecrawl.TravelEndsAt = TimeSpan.Zero;
                continue;
            }

            var duration = pipecrawl.TravelEndsAt - pipecrawl.TravelStartedAt;
            var elapsed = _timing.CurTime - pipecrawl.TravelStartedAt;
            var progress = duration <= TimeSpan.Zero
                ? 1f
                : Math.Clamp((float) (elapsed.TotalSeconds / duration.TotalSeconds), 0f, 1f);
            progress = EasingsExtensions.Ease(Easing.InOutCubic, progress);

            var origin = _transform.GetWorldPosition(currentTube);
            var destination = _transform.GetWorldPosition(nextTube);
            _transform.SetWorldPosition(uid, Vector2.Lerp(origin, destination, progress));

            if (_timing.CurTime < pipecrawl.TravelEndsAt)
                continue;

            pipecrawl.CurrentTube = nextTube;
            pipecrawl.NextTube = null;
            pipecrawl.PreviousDirection = pipecrawl.TravelDirection;
            pipecrawl.TravelDirection = Direction.Invalid;
            pipecrawl.TravelStartedAt = TimeSpan.Zero;
            pipecrawl.TravelEndsAt = TimeSpan.Zero;
            _transform.SetWorldPosition(uid, destination);
        }
    }

    private void OnPipecrawl(Entity<FelinidPipecrawlComponent> ent, ref FelinidPipecrawlActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Active)
        {
            ExitPipecrawlByAction(ent);
            args.Handled = true;
            return;
        }

        if (_timing.CurTime < ent.Comp.NextEntryAllowed)
            return;

        if (_inventory.TryGetSlotEntity(ent.Owner, ent.Comp.OuterClothingSlot, out _))
        {
            _popup.PopupEntity(Loc.GetString("felinid-pipecrawl-blocked-outer-clothing"), ent.Owner, ent.Owner);
            return;
        }

        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
        {
            return;
        }

        var owner = container.Owner;

        if (
            !TryComp<DisposalUnitComponent>(owner, out var disposal) ||
            !HasComp<FelinidPipecrawlEntryComponent>(owner) ||
            !Transform(owner).Anchored ||
            container.ID != DisposalUnitComponent.ContainerId ||
            disposal.Container.ContainedEntities.Count != 1)
        {
            return;
        }

        if (!TryEnterPipecrawl(ent, (owner, disposal)))
            return;

        args.Handled = true;
    }

    private void OnPipecrawlMoveInput(Entity<FelinidPipecrawlComponent> ent, ref MoveInputEvent args)
    {
        TryStartPipecrawlMove(ent, args.Entity.Comp.HeldMoveButtons);
    }

    private bool TryStartPipecrawlMove(Entity<FelinidPipecrawlComponent> ent, MoveButtons heldMoveButtons)
    {
        if (!ent.Comp.Active ||
            ent.Comp.CurrentTube == null ||
            ent.Comp.NextTube != null)
        {
            return false;
        }

        if (!TryComp<InputMoverComponent>(ent.Owner, out var mover))
            return false;

        var currentTube = ent.Comp.CurrentTube.Value;
        var direction = GetPipecrawlDirection(currentTube, mover, heldMoveButtons);

        if (direction == Direction.Invalid)
            return false;

        if (!TryComp<DisposalTubeComponent>(currentTube, out var tube) ||
            !_disposalTubeSystem.CanConnect(currentTube, tube, direction) ||
            _disposalTubeSystem.NextTubeFor(currentTube, direction, tube) is not { } nextTube)
        {
            return false;
        }

        ent.Comp.NextTube = nextTube;
        ent.Comp.TravelDirection = direction;
        ent.Comp.TravelStartedAt = _timing.CurTime;
        ent.Comp.TravelEndsAt = _timing.CurTime + ent.Comp.TransitTime;
        SetPipecrawlFacing(ent.Owner, currentTube, direction);
        return true;
    }

    private Direction GetPipecrawlDirection(
        EntityUid currentTube,
        InputMoverComponent mover,
        MoveButtons heldMoveButtons)
    {
        var buttons = SharedMoverController.GetNormalizedMovement(heldMoveButtons);
        var input = new Vector2(
            ((buttons & MoveButtons.Right) != 0 ? 1f : 0f) - ((buttons & MoveButtons.Left) != 0 ? 1f : 0f),
            ((buttons & MoveButtons.Up) != 0 ? 1f : 0f) - ((buttons & MoveButtons.Down) != 0 ? 1f : 0f));

        if (input == Vector2.Zero)
            return Direction.Invalid;

        var localDirection = _mover.GetParentGridAngle(mover).RotateVec(input);
        if (Transform(currentTube).GridUid is { } gridUid)
            localDirection = (-_transform.GetWorldRotation(gridUid)).RotateVec(localDirection);

        return Angle.FromWorldVec(localDirection).GetCardinalDir();
    }

    private void SetPipecrawlFacing(EntityUid uid, EntityUid currentTube, Direction direction)
    {
        var rotation = direction.ToAngle();
        if (Transform(currentTube).GridUid is { } gridUid)
            rotation += _transform.GetWorldRotation(gridUid);

        _transform.SetWorldRotation(uid, rotation);
    }

    private void OnPipecrawlMobStateChanged(Entity<FelinidPipecrawlComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.Active || args.NewMobState is not (MobState.Critical or MobState.Dead))
            return;

        SetPipecrawlCooldown(ent, ent.Comp.ExitCooldown);
        LaunchPipecrawlerThroughDisposals(ent);
    }

    private void LaunchPipecrawlerThroughDisposals(
        Entity<FelinidPipecrawlComponent> ent,
        Direction? preferredDirection = null)
    {
        var tube = ent.Comp.CurrentTube;
        var previousDirection = ent.Comp.PreviousDirection;
        if (ent.Comp.NextTube is { } nextTube && Exists(nextTube))
        {
            tube = nextTube;
            previousDirection = ent.Comp.TravelDirection;
        }

        if (tube is not { } tubeUid || !TryComp<DisposalTubeComponent>(tubeUid, out _))
        {
            TryForceExitPipecrawl(ent.AsNullable(), true);
            return;
        }

        SetPipecrawlActive(ent, false);
        _transform.SetWorldPosition(ent.Owner, _transform.GetWorldPosition(tubeUid));

        var holderUid = Spawn(DisposalHolderPrototype, Transform(tubeUid).Coordinates);
        var holder = Comp<DisposalHolderComponent>(holderUid);
        holder.PreviousDirection = preferredDirection ?? previousDirection;

        if (!_container.Insert(ent.Owner, holder.Container))
        {
            Del(holderUid);
            ReleasePipecrawlAir(ent);
            LaunchPipecrawler(ent.Owner);
            return;
        }

        _atmosphere.Merge(holder.Air, ent.Comp.Air);
        ent.Comp.Air.Clear();
        if (_disposableSystem.EnterTube(holderUid, tubeUid, holder) &&
            preferredDirection is { } direction)
        {
            holder.CurrentDirection = direction;
        }
    }

    private void OnEntityInsertedIntoContainer(EntGotInsertedIntoContainerMessage args)
    {
        if (HasComp<DisposalHolderComponent>(args.Container.Owner))
            EnsureComp<FelinidPipecrawlContentsComponent>(args.Entity);
    }

    private void OnPipeContentsRemoved(
        Entity<FelinidPipecrawlContentsComponent> ent,
        ref EntGotRemovedFromContainerMessage args)
    {
        if (HasComp<DisposalHolderComponent>(args.Container.Owner))
            RemCompDeferred<FelinidPipecrawlContentsComponent>(ent.Owner);
    }

    private void OnPipecrawlInhale(Entity<FelinidPipecrawlComponent> ent, ref InhaleLocationEvent args)
    {
        if (ent.Comp.Active)
            args.Gas ??= ent.Comp.Air;
    }

    private void OnPipecrawlExhale(Entity<FelinidPipecrawlComponent> ent, ref ExhaleLocationEvent args)
    {
        if (ent.Comp.Active)
            args.Gas ??= ent.Comp.Air;
    }

    private void OnPipecrawlGetAir(Entity<FelinidPipecrawlComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (!ent.Comp.Active || args.Handled)
            return;

        args.Gas = ent.Comp.Air;
        args.Handled = true;
    }

    private void SetPipecrawlActive(Entity<FelinidPipecrawlComponent> ent, bool active)
    {
        if (ent.Comp.Active == active)
            return;

        ent.Comp.Active = active;
        if (!active)
        {
            ent.Comp.CurrentTube = null;
            ent.Comp.NextTube = null;
            ent.Comp.PreviousDirection = Direction.Invalid;
            ent.Comp.TravelDirection = Direction.Invalid;
            ent.Comp.TravelStartedAt = TimeSpan.Zero;
            ent.Comp.TravelEndsAt = TimeSpan.Zero;
        }

        SetPipecrawlPhysics(ent.Owner, active);

        Dirty(ent);
        _sharedPipecrawl.RefreshPipecrawlState(ent);

        if (TryComp<BlindableComponent>(ent.Owner, out var blindable))
            _blindable.UpdateIsBlind((ent.Owner, blindable));

        if (HasComp<ContentEyeComponent>(ent.Owner) && HasComp<EyeComponent>(ent.Owner))
            _contentEye.UpdatePvsScale(ent.Owner);

        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private bool TryEnterPipecrawl(
        Entity<FelinidPipecrawlComponent> ent,
        Entity<DisposalUnitComponent> disposal)
    {
        var xform = Transform(disposal.Owner);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        EntityUid? entryUid = null;
        foreach (var local in _map.GetLocal(xform.GridUid!.Value, grid, xform.Coordinates))
        {
            if (!HasComp<DisposalEntryComponent>(local) ||
                !HasComp<DisposalTubeComponent>(local) ||
                !Transform(local).Anchored)
                continue;

            entryUid = local;
            break;
        }

        if (entryUid == null)
            return false;

        SetPipecrawlActive(ent, true);

        if (!_container.Remove(ent.Owner, disposal.Comp.Container))
        {
            SetPipecrawlActive(ent, false);
            return false;
        }

        SetPipecrawlPhysics(ent.Owner, true);
        ent.Comp.CurrentTube = entryUid;
        ent.Comp.NextTube = null;
        ent.Comp.PreviousDirection = Direction.Invalid;
        ent.Comp.TravelDirection = Direction.Invalid;
        ent.Comp.TravelStartedAt = TimeSpan.Zero;
        ent.Comp.TravelEndsAt = TimeSpan.Zero;
        _transform.SetWorldPosition(ent.Owner, _transform.GetWorldPosition(entryUid.Value));
        FillPipecrawlAir(disposal.Owner, ent);
        return true;
    }

    private void ExitPipecrawlByAction(Entity<FelinidPipecrawlComponent> ent)
    {
        var tube = ent.Comp.NextTube ?? ent.Comp.CurrentTube;
        if (tube is not { } tubeUid || !Exists(tubeUid))
        {
            TryForceExitPipecrawl(ent.AsNullable(), false);
            return;
        }

        if (HasComp<DisposalEntryComponent>(tubeUid))
        {
            if (!TryFindDisposalUnitAtEntry(tubeUid, out var disposalUnit))
            {
                _popup.PopupEntity(
                    Loc.GetString("felinid-pipecrawl-exit-no-disposal-unit"),
                    ent.Owner,
                    ent.Owner);
                return;
            }

            SetPipecrawlPhysics(ent.Owner, false);
            if (!_disposalUnitSystem.TryInsert(disposalUnit, ent.Owner, null))
            {
                SetPipecrawlPhysics(ent.Owner, true);
                _popup.PopupEntity(
                    Loc.GetString("felinid-pipecrawl-exit-blocked"),
                    ent.Owner,
                    ent.Owner);
                return;
            }

            SetPipecrawlCooldown(ent, ent.Comp.EnterCooldown);
            SetPipecrawlActive(ent, false);
            ReleasePipecrawlAir(ent);
            return;
        }

        SetPipecrawlCooldown(ent, ent.Comp.ExitCooldown);
        var preferredDirection = GetPreferredDisposalDirection(ent.Owner, tubeUid);
        LaunchPipecrawlerThroughDisposals(
            ent,
            preferredDirection == Direction.Invalid ? null : preferredDirection);
    }

    private bool TryFindDisposalUnitAtEntry(EntityUid? currentTube, out EntityUid disposalUnit)
    {
        disposalUnit = default;
        if (currentTube == null ||
            !HasComp<DisposalEntryComponent>(currentTube.Value))
        {
            return false;
        }

        var xform = Transform(currentTube.Value);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        foreach (var local in _map.GetLocal(xform.GridUid!.Value, grid, xform.Coordinates))
        {
            if (!HasComp<DisposalUnitComponent>(local) ||
                !HasComp<FelinidPipecrawlEntryComponent>(local) ||
                !Transform(local).Anchored)
                continue;

            disposalUnit = local;
            return true;
        }

        return false;
    }

    private Direction GetPreferredDisposalDirection(EntityUid uid, EntityUid tubeUid)
    {
        var facing = _transform.GetWorldRotation(uid);
        if (Transform(tubeUid).GridUid is { } gridUid)
            facing -= _transform.GetWorldRotation(gridUid);

        var facingVector = facing.ToWorldVec();
        var directions = new GetDisposalsConnectableDirectionsEvent();
        RaiseLocalEvent(tubeUid, ref directions);

        var bestDirection = Direction.Invalid;
        var bestDot = float.NegativeInfinity;
        foreach (var direction in directions.Connectable)
        {
            if (_disposalTubeSystem.NextTubeFor(tubeUid, direction) == null)
                continue;

            var dot = Vector2.Dot(direction.ToVec(), facingVector);
            if (dot <= bestDot)
                continue;

            bestDirection = direction;
            bestDot = dot;
        }

        return bestDirection;
    }

    private void SetPipecrawlCooldown(Entity<FelinidPipecrawlComponent> ent, TimeSpan duration)
    {
        ent.Comp.CooldownStartedAt = _timing.CurTime;
        ent.Comp.NextEntryAllowed = _timing.CurTime + duration;
        Dirty(ent);
    }

    private void FillPipecrawlAir(EntityUid source, Entity<FelinidPipecrawlComponent> ent)
    {
        ent.Comp.Air.Clear();
        if (_atmosphere.GetContainingMixture(source, ignoreExposed: true, excite: true) is { } environment)
            _atmosphere.Merge(ent.Comp.Air, environment.RemoveVolume(ent.Comp.Air.Volume));
    }

    private void ReleasePipecrawlAir(Entity<FelinidPipecrawlComponent> ent)
    {
        if (_atmosphere.GetContainingMixture(ent.Owner, ignoreExposed: true, excite: true) is { } environment)
            _atmosphere.Merge(environment, ent.Comp.Air);

        ent.Comp.Air.Clear();
    }

    private void SetPipecrawlPhysics(EntityUid uid, bool active)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        var isContained = _container.TryGetContainingContainer((uid, null, null), out _);
        _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        _physics.SetCanCollide(uid, !active && !isContained, body: physics);
    }

    public bool TryForceExitPipecrawl(Entity<FelinidPipecrawlComponent?> ent, bool launch)
    {
        if (!Resolve(ent, ref ent.Comp, false) || !ent.Comp.Active)
            return false;

        var currentTube = ent.Comp.CurrentTube;
        SetPipecrawlActive((ent.Owner, ent.Comp), false);

        if (currentTube != null && Exists(currentTube.Value))
            _transform.SetWorldPosition(ent.Owner, _transform.GetWorldPosition(currentTube.Value));

        ReleasePipecrawlAir((ent.Owner, ent.Comp));

        if (launch)
            LaunchPipecrawler(ent.Owner);

        return true;
    }

    private void LaunchPipecrawler(EntityUid uid)
    {
        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);

        _throwing.TryThrow(uid, _transform.GetWorldRotation(uid).ToWorldVec() * 3f, 10f);
    }
}
