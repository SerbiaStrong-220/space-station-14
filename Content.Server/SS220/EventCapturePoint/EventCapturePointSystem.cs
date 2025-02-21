// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Server.SS220.FractWar;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.SS220.EventCapturePoint;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.EventCapturePoint;

public sealed class EventCapturePointSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly FractWarRuleSystem _fractWarRule = default!;

    private const float RefreshWinPointsRate = 60f;

    private TimeSpan _nextRefreshWinPoints = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventCapturePointComponent, ActivateInWorldEvent>(OnActivated);
        SubscribeLocalEvent<EventCapturePointComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EventCapturePointComponent, ComponentShutdown>(OnPointShutdown);

        SubscribeLocalEvent<EventCapturePointFlagComponent, GettingPickedUpAttemptEvent>(OnFlagPickupAttempt);
        SubscribeLocalEvent<EventCapturePointComponent, FlagInstallationFinshedEvent>(OnFlagInstalled);
        SubscribeLocalEvent<EventCapturePointComponent, FlagRemovalFinshedEvent>(OnFlagRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var gameRule = _fractWarRule.GetActiveGameRule();
        if (gameRule is null)
            return;

        var query = EntityQueryEnumerator<EventCapturePointComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (component.FlagEntity is not { } flagUid ||
                !TryComp<EventCapturePointFlagComponent>(flagUid, out var flagComp) ||
                flagComp.Fraction is not { } flagFraction)
                continue;

            if (!component.PointRetentionTime.TryAdd(flagFraction, TimeSpan.Zero))
                component.PointRetentionTime[flagFraction] += TimeSpan.FromSeconds(frameTime);
        }

        if (_timing.CurTime >= _nextRefreshWinPoints)
        {
            RefreshWinPoints(gameRule);
            _nextRefreshWinPoints = _timing.CurTime + TimeSpan.FromSeconds(RefreshWinPointsRate);
        }
    }

    #region Listeners
    private void OnActivated(Entity<EventCapturePointComponent> entity, ref ActivateInWorldEvent args)
    {
        if (entity.Comp.FlagEntity.HasValue)
        {
            RemoveFlag(entity, args.User);
        }
    }

    private void OnFlagInstalled(Entity<EventCapturePointComponent> entity, ref FlagInstallationFinshedEvent args)
    {
        if (args.Cancelled)
            return;

        if (!args.Used.HasValue)
            return;

        AddFlagInstantly(entity, args.Used.Value);
    }

    private void OnFlagPickupAttempt(Entity<EventCapturePointFlagComponent> entity, ref GettingPickedUpAttemptEvent args)
    {
        if (entity.Comp.Planted)
            args.Cancel();
    }

    private void OnFlagRemoved(Entity<EventCapturePointComponent> entity, ref FlagRemovalFinshedEvent args)
    {
        if (args.Cancelled)
            return;

        RemoveFlagInstantly(entity);
    }

    private void OnPointShutdown(Entity<EventCapturePointComponent> entity, ref ComponentShutdown args)
    {
        RefreshWinPointsFromCapturePoint(entity.Comp);

        if (entity.Comp.FlagEntity.HasValue &&
            entity.Comp.FlagEntity.Value.Valid &&
            EntityManager.EntityExists(entity.Comp.FlagEntity.Value))
        {
            RemoveFlagInstantly(entity);
        }
    }

    private void OnInteractUsing(Entity<EventCapturePointComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<EventCapturePointFlagComponent>(args.Used))
            return;

        AddOrRemoveFlag(entity, args.User, args.Used);
    }
    #endregion

    public void RemoveFlagInstantly(Entity<EventCapturePointComponent> entity)
    {
        if (entity.Comp.FlagEntity is not { } flag)
            return;

        if (TryComp<EventCapturePointFlagComponent>(flag, out var flagComp))
            flagComp.Planted = false;

        _transform.SetParent(flag, _transform.GetParentUid(entity));
        _appearance.SetData(flag, CaptureFlagVisuals.Visuals, false);
        _appearance.SetData(entity, CapturePointVisuals.Visuals, false);
        entity.Comp.FlagEntity = null;

        if (TryComp<PhysicsComponent>(flag, out var physComp))
        {
            _physics.SetBodyType(flag, BodyType.Dynamic, body: physComp);

            var maxAxisImp = entity.Comp.FlagRemovalImpulse;
            var impulseVec = new Vector2(_random.NextFloat(-maxAxisImp, maxAxisImp), _random.NextFloat(-maxAxisImp, maxAxisImp));
            _physics.ApplyLinearImpulse(flag, impulseVec);
        }
    }

    public void AddFlagInstantly(Entity<EventCapturePointComponent> entity, EntityUid flag)
    {
        if (!TryComp<EventCapturePointFlagComponent>(flag, out var flagComp))
            return;

        _container.TryRemoveFromContainer(flag, true);
        var xform = EnsureComp<TransformComponent>(flag);
        var coords = new EntityCoordinates(entity, Vector2.Zero);
        _transform.SetCoordinates(flag, xform, coords);
        _transform.SetLocalRotationNoLerp(flag, Angle.Zero, xform);
        // We don't anchor an entity because that will lead to an unapplicable component state
        // because we can't remove an anchored entity from container
        flagComp.Planted = true;

        _appearance.SetData(flag, CaptureFlagVisuals.Visuals, true);
        _appearance.SetData(entity, CapturePointVisuals.Visuals, true);

        entity.Comp.FlagEntity = flag;

        if (TryComp<PhysicsComponent>(flag, out var physComp))
            _physics.SetBodyType(flag, BodyType.Static, body: physComp);
    }

    public void AddFlag(Entity<EventCapturePointComponent> entity, EntityUid user, EntityUid newFlag)
    {
        var flagEvent = new FlagInstallationFinshedEvent();

        var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.FlagManipulationDuration, flagEvent, entity, target: entity, used: newFlag)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    public void RemoveFlag(Entity<EventCapturePointComponent> entity, EntityUid user)
    {
        var flagEvent = new FlagRemovalFinshedEvent();

        var doAfterArgs = new DoAfterArgs(EntityManager, user, entity.Comp.FlagManipulationDuration, flagEvent, entity, target: entity)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    public void AddOrRemoveFlag(Entity<EventCapturePointComponent> entity, EntityUid user, EntityUid newFlag)
    {
        if (entity.Comp.FlagEntity == newFlag)
            return;

        if (entity.Comp.FlagEntity.HasValue)
            RemoveFlag(entity, user);
        else
            AddFlag(entity, user, newFlag);
    }

    public void RefreshWinPoints(FractWarRuleComponent? gameRule = null)
    {
        gameRule ??= _fractWarRule.GetActiveGameRule();
        if (gameRule is null)
            return;

        var query = EntityQueryEnumerator<EventCapturePointComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            RefreshWinPointsFromCapturePoint(comp, gameRule);
        }
    }

    public void RefreshWinPointsFromCapturePoint(EventCapturePointComponent comp, FractWarRuleComponent? gameRule = null)
    {
        gameRule ??= _fractWarRule.GetActiveGameRule();

        if (gameRule is null)
            return;

        foreach (var (fraction, retTime) in comp.PointRetentionTime)
        {
            var wp = comp.WinPointsCoefficient * (float)(retTime.TotalSeconds / comp.RetentionTimeForWinPoint.TotalSeconds);

            if (!gameRule.FractionsWinPoints.TryAdd(fraction, wp))
                gameRule.FractionsWinPoints[fraction] += wp;

            comp.PointRetentionTime[fraction] = TimeSpan.Zero;
        }
    }
}
