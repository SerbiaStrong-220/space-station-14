// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Ghost;
using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Station.Components;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.SS220.CultYogg.Nyarlathotep.Components;
using Content.Server.SS220.CultYogg.Nyarlathotep.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.Nyarlathotep;


namespace Content.Server.SS220.CultYogg.Nyarlathotep;

/// <summary>
/// The entity system primarily responsible for managing <see cref="NyarlathotepHorizonComponent"/>s.
/// Handles their consumption of entities.
/// </summary>
public sealed class NyarlathotepHorizonSystem : SharedNyarlathotepHorizonSystem
{
    #region Dependencies
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    #endregion Dependencies

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NyarlathotepHorizonComponent, MapInitEvent>(OnHorizonMapInit);
        SubscribeLocalEvent<NyarlathotepHorizonComponent, StartCollideEvent>(OnStartCollide);

        SubscribeLocalEvent<MapGridComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<GhostComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<StationDataComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<MobStateComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsumeMobs);

        SubscribeLocalEvent<ContainerManagerComponent, NyarlathotepHorizonConsumedEntityEvent>(OnContainerConsumed);
    }

    private void OnHorizonMapInit(Entity<NyarlathotepHorizonComponent> component, ref MapInitEvent args)
    {
        component.Comp.NextConsumeWaveTime = _timing.CurTime;
    }

    /// <summary>
    /// Updates the cooldowns of Nyarlathotep horizon.
    /// If a horizon are off cooldown this makes it consume everything within range and resets their cooldown.
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NyarlathotepHorizonComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var nyarlathotepHorizon, out var xform))
        {
            var curTime = _timing.CurTime;
            if (nyarlathotepHorizon.NextConsumeWaveTime <= curTime)
                UpdateHorizon((uid, nyarlathotepHorizon), xform);
        }
    }

    /// <summary>
    /// Makes a horizon consume everything nearby and resets the cooldown it for the next automated wave.
    /// </summary>
    private void UpdateHorizon(Entity<NyarlathotepHorizonComponent> nyarlathotepHorizon, TransformComponent? xform = null)
    {
        if(!HasComp<NyarlathotepHorizonComponent>(nyarlathotepHorizon))
            return;

        nyarlathotepHorizon.Comp.NextConsumeWaveTime += nyarlathotepHorizon.Comp.TargetConsumePeriod;

        if (!Resolve(nyarlathotepHorizon.Owner, ref xform))
            return;

        if (_containerSystem.TryGetContainingContainer(nyarlathotepHorizon.Owner, out var container)
        && !AttemptConsumeEntity(nyarlathotepHorizon.Owner, container.Owner, nyarlathotepHorizon))
            ConsumeEntitiesInContainer(nyarlathotepHorizon.Owner, container, nyarlathotepHorizon, container);
    }

    /// <summary>
    /// Makes a horizon consume a given entity.
    /// </summary>
    private void ConsumeEntity(EntityUid nyarlathotep, EntityUid entityToConsume, NyarlathotepHorizonComponent nyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        if (!EntityManager.IsQueuedForDeletion(entityToConsume)
        && (HasComp<MindContainerComponent>(entityToConsume)
            || _tagSystem.HasTag(entityToConsume, "HighRiskItem")))
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.Extreme, $"{ToPrettyString(entityToConsume)} entered the event horizon of {ToPrettyString(nyarlathotep)} and was deleted");
        }

        EntityManager.QueueDeleteEntity(entityToConsume);
        var evEaten = new NyarlathotepHorizonConsumedEntityEvent(entityToConsume, nyarlathotep, nyarlathotepHorizon, outerContainer);
        RaiseLocalEvent(entityToConsume, ref evEaten);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a given entity.
    /// </summary>
    private bool AttemptConsumeEntity(EntityUid nyarlathotep, EntityUid entityToConsume, NyarlathotepHorizonComponent nyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        if (!CanConsumeEntity(nyarlathotep, entityToConsume, nyarlathotepHorizon))
            return false;

        ConsumeEntity(nyarlathotep, entityToConsume, nyarlathotepHorizon, outerContainer);
        return true;
    }

    /// <summary>
    /// Checks whether a horizon can consume a given entity.
    /// </summary>
    private bool CanConsumeEntity(EntityUid nyarlathotep, EntityUid entityToConsume, NyarlathotepHorizonComponent nyarlathotepHorizon)
    {
        var ev = new NyarlathotepHorizonAttemptConsumeEntityEvent(entityToConsume, nyarlathotep, nyarlathotepHorizon);
        RaiseLocalEvent(entityToConsume, ref ev);
        return !ev.Cancelled;
    }

    /// <summary>
    /// Attempts to consume all entities within a given distance of an entity;
    /// Excludes the center entity.
    /// </summary>
    public void ConsumeEntitiesInRange(EntityUid nyarlathotep, float range, TransformComponent? xform = null, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(nyarlathotep, ref xform, ref nyarlathotepHorizon))
            return;

        var range2 = range * range;
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var epicenter = _xformSystem.GetWorldPosition(xform, xformQuery);
        foreach (var entity in _lookup.GetEntitiesInRange(_xformSystem.GetMapCoordinates(nyarlathotep, xform), range, flags: LookupFlags.Uncontained))
        {
            if (entity == nyarlathotep)
                continue;
            if (!xformQuery.TryGetComponent(entity, out var entityXform))
                continue;

            var displacement = _xformSystem.GetWorldPosition(entityXform, xformQuery) - epicenter;
            if (displacement.LengthSquared() > range2)
                continue;

            AttemptConsumeEntity(nyarlathotep, entity, nyarlathotepHorizon);
        }
    }

    /// <summary>
    /// Attempts to consume all entities within a container.
    /// Excludes the horizon itself.
    /// All immune entities within the container will be dumped to a given container or the map/grid if that is impossible.
    /// </summary>
    private void ConsumeEntitiesInContainer(EntityUid nyarlathotep, BaseContainer container, NyarlathotepHorizonComponent nyarlathotepHorizon, BaseContainer? outerContainer = null)
    {
        List<EntityUid> immune = new();

        foreach (var entity in container.ContainedEntities)
        {
            if (entity == nyarlathotep || !AttemptConsumeEntity(nyarlathotep, entity, nyarlathotepHorizon, outerContainer))
                immune.Add(entity);
        }

        if (outerContainer == container || immune.Count <= 0)
            return;
        foreach (var entity in immune)
        {
            var targetContainer = outerContainer;
            while (targetContainer != null)
            {
                if (_containerSystem.Insert(entity, targetContainer))
                    break;

                _containerSystem.TryGetContainingContainer(targetContainer.Owner, out targetContainer);
            }
            if (targetContainer == null)
                _xformSystem.AttachToGridOrMap(entity);
        }
    }

    #region Getters/Setters

    /// <summary>
    /// Sets how often a horizon will scan for overlapping entities to consume.
    /// The value is specifically how long the subsystem should wait between scans.
    /// If the new scanning period would have already prompted a scan given the previous scan time one is prompted immediately.
    /// </summary>
    public void SetConsumePeriod(EntityUid nyarlathotep, TimeSpan value, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(nyarlathotep, ref nyarlathotepHorizon))
            return;

        if (MathHelper.CloseTo(nyarlathotepHorizon.TargetConsumePeriod.TotalSeconds, value.TotalSeconds))
            return;

        var diff = (value - nyarlathotepHorizon.TargetConsumePeriod);
        nyarlathotepHorizon.TargetConsumePeriod = value;
        nyarlathotepHorizon.NextConsumeWaveTime += diff;

        var curTime = _timing.CurTime;
        if (nyarlathotepHorizon.NextConsumeWaveTime < curTime)
            UpdateHorizon((nyarlathotep, nyarlathotepHorizon));
    }

    #endregion Getters/Setters

    #region Event Handlers

    /// <summary>
    /// Prevents a Nyarlathotep from colliding with anything it is incapable of consuming.
    /// </summary>
    protected override bool PreventCollide(Entity<NyarlathotepHorizonComponent> comp, ref PreventCollideEvent args)
    {
        if (base.PreventCollide(comp, ref args) || args.Cancelled)
            return true;

        args.Cancelled = args.OurFixture.Hard && CanConsumeEntity(comp.Owner, args.OtherEntity, comp.Comp);
        return false;
    }

    /// <summary>
    /// An event handler that prevents Nyarlathotep from consuming living entities, instead we just deal damage to them.
    /// This is also the logic for preventing MiGo damage.
    /// </summary>
    private void PreventConsumeMobs(Entity<MobStateComponent> comp, ref NyarlathotepHorizonAttemptConsumeEntityEvent args)
    {
        PreventConsume(comp.Owner, comp.Comp, ref args);
        if (_mob.IsAlive(args.entity) && !HasComp<MiGoComponent>(args.entity))
            _bodySystem.GibBody(comp.Owner);
    }

    /// <summary>
    /// A generic event handler that prevents Nyarlathotep from consuming entities with a component of a given type if registered.
    /// </summary>
    private static void PreventConsume<TComp>(EntityUid uid, TComp comp, ref NyarlathotepHorizonAttemptConsumeEntityEvent args)
    {
        if (!args.Cancelled)
            args.Cancelled = true;
    }

    /// <summary>
    /// Handles horizons consuming any entities they bump into.
    /// </summary>
    private void OnStartCollide(Entity<NyarlathotepHorizonComponent> comp, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != comp.Comp.ColliderFixtureId)
            return;

        AttemptConsumeEntity(comp.Owner, args.OtherEntity, comp.Comp);
    }

    /// <summary>
    /// Recursively consumes all entities within a container that is consumed by Nyarlathotep.
    /// If an entity within a consumed container cannot be consumed itself it is removed from the container.
    /// </summary>
    private void OnContainerConsumed(Entity<ContainerManagerComponent> comp, ref NyarlathotepHorizonConsumedEntityEvent args)
    {
        var dropContainer = args.Container;
        if (dropContainer is null)
            _containerSystem.TryGetContainingContainer(comp.Owner, out dropContainer);

        foreach (var container in comp.Comp.GetAllContainers())
        {
            ConsumeEntitiesInContainer(args.NyarlathotepHorizonUid, container, args.NyarlathotepHorizon, dropContainer);
        }
    }
    #endregion Event Handlers
}
