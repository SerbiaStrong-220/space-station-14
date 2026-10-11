using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Unit;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.SS220.Maths;
using Content.Shared.Spillable;
using Content.Shared.Throwing;
using Content.Shared.Tools.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Felinid;

public sealed partial class DisposalPipeCrawlerSystem : SharedDisposalPipeCrawlerSystem
{
    private static readonly EntProtoId DisposalHolderPrototype = "DisposalHolder";
    private const float LaunchDistance = 3f;
    private const float LaunchSpeed = 10f;

    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedContentEyeSystem _contentEye = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private DisposableSystem _disposableSystem = default!;
    [Dependency] private DisposalUnitSystem _disposalUnitSystem = default!;
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private DisposalTubeSystem _disposalTubeSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, DisposalPipeCrawlerActionEvent>(OnPipecrawl);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, InhaleLocationEvent>(OnPipecrawlInhale);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, ExhaleLocationEvent>(OnPipecrawlExhale);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, AtmosExposedGetAirEvent>(OnPipecrawlGetAir);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, MoveInputEvent>(OnPipecrawlMoveInput);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, MobStateChangedEvent>(OnPipecrawlMobStateChanged);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, SpillAttemptEvent>(OnCrawlerSpillAttempt);
        SubscribeLocalEvent<BeingDisposedComponent, DropAttemptEvent>(OnDisposedCrawlerDropAttempt);
        SubscribeLocalEvent<DisposalTubeComponent, InteractUsingEvent>(OnTubeInteractUsing);
        SubscribeLocalEvent<DisposalTubeComponent, DisposalPipeExtractionDoAfterEvent>(OnTubeExtractionDoAfter);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DisposalPipeCrawlerComponent>();
        while (query.MoveNext(out var uid, out var pipecrawl))
        {
            if (!pipecrawl.InsidePipe)
                continue;

            if (pipecrawl.CurrentTube is not { } currentTube || !Exists(currentTube))
            {
                ForceExitPipecrawl((uid, pipecrawl), false);
                continue;
            }

            if (pipecrawl.NextTube == null && TryComp<InputMoverComponent>(uid, out var mover))
                TryStartPipecrawlMove((uid, pipecrawl), mover.HeldMoveButtons);

            if (pipecrawl.NextTube is not { } nextTube)
                continue;

            if (!Exists(nextTube))
            {
                ResetTravel(pipecrawl);
                continue;
            }

            var duration = pipecrawl.TravelEndsAt - pipecrawl.TravelStartedAt;
            var elapsed = _timing.CurTime - pipecrawl.TravelStartedAt;
            var progress = duration <= TimeSpan.Zero
                ? 1f
                : Math.Clamp((float) (elapsed / duration), 0f, 1f);
            progress = EasingsExtensions.Ease(Easing.InOutCubic, progress);

            var origin = _transform.GetWorldPosition(currentTube);
            var destination = _transform.GetWorldPosition(nextTube);
            _transform.SetWorldPosition(uid, Vector2.Lerp(origin, destination, progress));

            if (_timing.CurTime < pipecrawl.TravelEndsAt)
                continue;

            pipecrawl.CurrentTube = nextTube;
            Dirty(uid, pipecrawl);
            pipecrawl.PreviousDirection = pipecrawl.TravelDirection;
            ResetTravel(pipecrawl);
        }
    }

    private void OnPipecrawl(Entity<DisposalPipeCrawlerComponent> ent, ref DisposalPipeCrawlerActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.InsidePipe)
        {
            ExitPipecrawlByAction(ent);
            args.Handled = true;
            SetActionUseDelay(ent, args.Action);
            return;
        }

        if (_timing.CurTime < ent.Comp.NextEntryAllowed)
            return;

        if (_inventory.TryGetSlotEntity(ent.Owner, "outerClothing", out _))
        {
            _popup.PopupEntity(Loc.GetString("disposal-pipe-crawler-blocked-outer-clothing"), ent.Owner, ent.Owner);
            return;
        }

        if (!TryGetContainingDisposalUnit(ent.Owner, out var disposal) ||
            !Transform(disposal.Value.Owner).Anchored ||
            disposal.Value.Comp.Container.ContainedEntities.Count != 1)
        {
            return;
        }

        args.Handled = TryEnterPipecrawl(ent, disposal.Value);
        if (args.Handled)
            SetActionUseDelay(ent, args.Action);
    }

    /// <summary>
    /// The actions system restarts the action's own use delay right after this handler,
    /// so hand it the pipe cooldown instead of setting a cooldown it would overwrite.
    /// </summary>
    private void SetActionUseDelay(Entity<DisposalPipeCrawlerComponent> ent, Entity<ActionComponent> action)
    {
        TimeSpan? delay = ent.Comp.InsidePipe ? null : ent.Comp.NextEntryAllowed - _timing.CurTime;
        _actions.SetUseDelay(action.AsNullable(), delay);
    }

    private void OnPipecrawlMoveInput(Entity<DisposalPipeCrawlerComponent> ent, ref MoveInputEvent args)
    {
        TryStartPipecrawlMove(ent, args.Entity.Comp.HeldMoveButtons);
    }

    private bool TryStartPipecrawlMove(Entity<DisposalPipeCrawlerComponent> ent, MoveButtons heldMoveButtons)
    {
        if (!ent.Comp.InsidePipe ||
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

    private void OnPipecrawlMobStateChanged(Entity<DisposalPipeCrawlerComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.InsidePipe || args.NewMobState is not (MobState.Critical or MobState.Dead))
            return;

        SetPipecrawlCooldown(ent, ent.Comp.ExitCooldown);
        LaunchPipecrawlerThroughDisposals(ent);
    }

    private void LaunchPipecrawlerThroughDisposals(
        Entity<DisposalPipeCrawlerComponent> ent,
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
            ForceExitPipecrawl(ent, true);
            return;
        }

        SetPipecrawlActive(ent, false);
        _transform.SetWorldPosition(ent.Owner, _transform.GetWorldPosition(tubeUid));

        var holderUid = Spawn(DisposalHolderPrototype, new EntityCoordinates(tubeUid, Vector2.Zero));
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

    private void OnCrawlerSpillAttempt(Entity<DisposalPipeCrawlerComponent> ent, ref SpillAttemptEvent args)
    {
        if (ent.Comp.InsidePipe || HasComp<BeingDisposedComponent>(ent.Owner))
            args.Cancelled = true;
    }

    private void OnDisposedCrawlerDropAttempt(Entity<BeingDisposedComponent> ent, ref DropAttemptEvent args)
    {
        if (HasComp<DisposalPipeCrawlerComponent>(ent.Owner))
            args.Cancel();
    }

    private void OnTubeInteractUsing(Entity<DisposalTubeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryFindPipecrawlerInTube(ent.Owner, out var crawler))
            return;

        args.Handled = _tool.UseTool(
            args.Used,
            args.User,
            ent.Owner,
            crawler.Value.Comp.ExtractionDelay,
            crawler.Value.Comp.ExtractionQuality,
            new DisposalPipeExtractionDoAfterEvent());
    }

    private void OnTubeExtractionDoAfter(Entity<DisposalTubeComponent> ent, ref DisposalPipeExtractionDoAfterEvent args)
    {
        if (!args.Cancelled && TryFindPipecrawlerInTube(ent.Owner, out var crawler))
            ForceExitPipecrawl(crawler.Value, false);
    }

    private bool TryFindPipecrawlerInTube(
        EntityUid tube,
        [NotNullWhen(true)] out Entity<DisposalPipeCrawlerComponent>? crawler)
    {
        var query = EntityQueryEnumerator<DisposalPipeCrawlerComponent>();
        while (query.MoveNext(out var uid, out var pipecrawl))
        {
            if (pipecrawl.InsidePipe && (pipecrawl.CurrentTube == tube || pipecrawl.NextTube == tube))
            {
                crawler = (uid, pipecrawl);
                return true;
            }
        }

        crawler = null;
        return false;
    }

    private void OnPipecrawlInhale(Entity<DisposalPipeCrawlerComponent> ent, ref InhaleLocationEvent args)
    {
        if (ent.Comp.InsidePipe)
            args.Gas ??= ent.Comp.Air;
    }

    private void OnPipecrawlExhale(Entity<DisposalPipeCrawlerComponent> ent, ref ExhaleLocationEvent args)
    {
        if (ent.Comp.InsidePipe)
            args.Gas ??= ent.Comp.Air;
    }

    private void OnPipecrawlGetAir(Entity<DisposalPipeCrawlerComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (!ent.Comp.InsidePipe || args.Handled)
            return;

        args.Gas = ent.Comp.Air;
        args.Handled = true;
    }

    private void SetPipecrawlActive(Entity<DisposalPipeCrawlerComponent> ent, bool active)
    {
        if (ent.Comp.InsidePipe == active)
            return;

        ent.Comp.InsidePipe = active;
        if (!active)
        {
            ent.Comp.CurrentTube = null;
            ent.Comp.PreviousDirection = Direction.Invalid;
            ResetTravel(ent.Comp);
        }

        SetPipecrawlPhysics(ent.Owner, active);

        Dirty(ent);
        RefreshPipecrawlState(ent);

        if (TryComp<BlindableComponent>(ent.Owner, out var blindable))
            _blindable.UpdateIsBlind((ent.Owner, blindable));

        if (HasComp<ContentEyeComponent>(ent.Owner) && HasComp<EyeComponent>(ent.Owner))
            _contentEye.UpdatePvsScale(ent.Owner);

        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private static void ResetTravel(DisposalPipeCrawlerComponent pipecrawl)
    {
        pipecrawl.NextTube = null;
        pipecrawl.TravelDirection = Direction.Invalid;
        pipecrawl.TravelStartedAt = TimeSpan.Zero;
        pipecrawl.TravelEndsAt = TimeSpan.Zero;
    }

    private bool TryEnterPipecrawl(
        Entity<DisposalPipeCrawlerComponent> ent,
        Entity<DisposalUnitComponent> disposal)
    {
        var xform = Transform(disposal.Owner);
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return false;

        var entry = _map.GetLocal(xform.GridUid.Value, grid, xform.Coordinates)
            .FirstOrDefault(uid => HasComp<DisposalEntryComponent>(uid) && HasComp<DisposalTubeComponent>(uid));
        if (entry == default)
            return false;

        SetPipecrawlActive(ent, true);

        if (!_container.Remove(ent.Owner, disposal.Comp.Container))
        {
            SetPipecrawlActive(ent, false);
            return false;
        }

        // Leaving the unit's container re-enables collision, so disable it again.
        SetPipecrawlPhysics(ent.Owner, true);
        ent.Comp.CurrentTube = entry;
        Dirty(ent);
        _transform.SetWorldPosition(ent.Owner, _transform.GetWorldPosition(entry));
        FillPipecrawlAir(disposal.Owner, ent);
        return true;
    }

    private void ExitPipecrawlByAction(Entity<DisposalPipeCrawlerComponent> ent)
    {
        var tube = ent.Comp.NextTube ?? ent.Comp.CurrentTube;
        if (tube is not { } tubeUid || !Exists(tubeUid))
        {
            ForceExitPipecrawl(ent, false);
            return;
        }

        if (HasComp<DisposalEntryComponent>(tubeUid))
        {
            if (!TryFindDisposalUnitAt(tubeUid, out var disposalUnit))
            {
                _popup.PopupEntity(
                    Loc.GetString("disposal-pipe-crawler-exit-no-disposal-unit"),
                    ent.Owner,
                    ent.Owner);
                return;
            }

            SetPipecrawlPhysics(ent.Owner, false);
            if (!_disposalUnitSystem.TryInsert(disposalUnit.Value, ent.Owner, null))
            {
                SetPipecrawlPhysics(ent.Owner, true);
                _popup.PopupEntity(
                    Loc.GetString("disposal-pipe-crawler-exit-blocked"),
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

    private bool TryFindDisposalUnitAt(EntityUid entry, [NotNullWhen(true)] out EntityUid? disposalUnit)
    {
        disposalUnit = null;
        var xform = Transform(entry);
        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return false;

        foreach (var uid in _map.GetLocal(xform.GridUid.Value, grid, xform.Coordinates))
        {
            if (!HasComp<DisposalUnitComponent>(uid))
                continue;

            disposalUnit = uid;
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

    private void SetPipecrawlCooldown(Entity<DisposalPipeCrawlerComponent> ent, TimeSpan duration)
    {
        ent.Comp.CooldownStartedAt = _timing.CurTime;
        ent.Comp.NextEntryAllowed = _timing.CurTime + duration;
        Dirty(ent);
    }

    private void FillPipecrawlAir(EntityUid source, Entity<DisposalPipeCrawlerComponent> ent)
    {
        ent.Comp.Air.Clear();
        if (_atmosphere.GetContainingMixture(source, ignoreExposed: true, excite: true) is { } environment)
            _atmosphere.Merge(ent.Comp.Air, environment.RemoveVolume(ent.Comp.Air.Volume));
    }

    private void ReleasePipecrawlAir(Entity<DisposalPipeCrawlerComponent> ent)
    {
        if (_atmosphere.GetContainingMixture(ent.Owner, ignoreExposed: true, excite: true) is { } environment)
            _atmosphere.Merge(environment, ent.Comp.Air);

        ent.Comp.Air.Clear();
    }

    private void SetPipecrawlPhysics(EntityUid uid, bool active)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        var isContained = _container.TryGetContainingContainer(uid, out _);
        _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        _physics.SetCanCollide(uid, !active && !isContained, body: physics);
    }

    private void ForceExitPipecrawl(Entity<DisposalPipeCrawlerComponent> ent, bool launch)
    {
        if (!ent.Comp.InsidePipe)
            return;

        var currentTube = ent.Comp.CurrentTube;
        SetPipecrawlActive(ent, false);

        if (currentTube != null && Exists(currentTube.Value))
            _transform.SetWorldPosition(ent.Owner, _transform.GetWorldPosition(currentTube.Value));

        ReleasePipecrawlAir(ent);

        if (launch)
            LaunchPipecrawler(ent.Owner);
    }

    private void LaunchPipecrawler(EntityUid uid)
    {
        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);

        _throwing.TryThrow(uid, _transform.GetWorldRotation(uid).ToWorldVec() * LaunchDistance, LaunchSpeed);
    }
}
