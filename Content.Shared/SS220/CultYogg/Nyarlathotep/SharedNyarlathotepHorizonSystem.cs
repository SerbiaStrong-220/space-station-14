// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

using Content.Shared.Ghost;
using Robust.Shared.Physics;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.EntitySystems;

/// <summary>
/// The general part of the Nyarlathotep system is primarily responsible for consuming entities <see cref="NyarlathotepHorizonComponent"/>s.
/// </summary>
public abstract class SharedNyarlathotepHorizonSystem : EntitySystem
{

    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly IViewVariablesManager Vvm = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NyarlathotepHorizonComponent, ComponentStartup>(OnNyarlathotepHorizonStartup);
        SubscribeLocalEvent<NyarlathotepHorizonComponent, PreventCollideEvent>(OnPreventCollide);

        var vvHandle = Vvm.GetTypeHandler<NyarlathotepHorizonComponent>();
        vvHandle.AddPath(nameof(NyarlathotepHorizonComponent.Radius), (_, comp) => comp.Radius, (uid, value, comp) => SetRadius(uid, value, nyarlathotepHorizon: comp));
        vvHandle.AddPath(nameof(NyarlathotepHorizonComponent.ColliderFixtureId), (_, comp) => comp.ColliderFixtureId, (uid, value, comp) => SetColliderFixtureId(uid, value, nyarlathotepHorizon: comp));
        vvHandle.AddPath(nameof(NyarlathotepHorizonComponent.ConsumerFixtureId), (_, comp) => comp.ConsumerFixtureId, (uid, value, comp) => SetConsumerFixtureId(uid, value, nyarlathotepHorizon: comp));
    }

    public override void Shutdown()
    {
        var vvHandle = Vvm.GetTypeHandler<NyarlathotepHorizonComponent>();
        vvHandle.RemovePath(nameof(NyarlathotepHorizonComponent.Radius));
        vvHandle.RemovePath(nameof(NyarlathotepHorizonComponent.ColliderFixtureId));
        vvHandle.RemovePath(nameof(NyarlathotepHorizonComponent.ConsumerFixtureId));

        base.Shutdown();
    }
    #region Getters/Setters

    /// <summary>
    /// Setter for <see cref="NyarlathotepHorizonComponent.Radius"/>
    /// May also update the fixture associated with the Nyarlathotep horizon.
    /// </summary>
    /// <param name="uid">The uid of Nyarlathotept change the horizon radius.</param>
    /// <param name="value">The new radius of the horizon.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing the radius of the horizon.</param>
    /// <param name="nyarlathotepHorizon">The state of the Nyarlathotepto horizon to change the radius of.</param>
    public void SetRadius(EntityUid uid, float value, bool updateFixture = true, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref nyarlathotepHorizon))
            return;

        var oldValue = nyarlathotepHorizon.Radius;
        if (value == oldValue)
            return;

        nyarlathotepHorizon.Radius = value;
        Dirty(uid, nyarlathotepHorizon);
        if (updateFixture)
            UpdateNyarlathotepHorizonFixture(uid, nyarlathotepHorizon: nyarlathotepHorizon);
    }

    /// <summary>
    /// Setter for <see cref="NyarlathotepHorizonComponent.HorizonFixtureId"/>
    /// May also update the fixture associated with the Nyarlathotep horizon.
    /// </summary>
    /// <param name="uid">The uid of the Nyarlathotep horizon with the fixture ID to change.</param>
    /// <param name="value">The new fixture ID to associate the Nyarlathotep horizon with.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing Nyarlathotep horizon.</param>
    /// <param name="nyarlathotepHorizon">The state of the Nyarlathotep horizon with the fixture ID to change.</param>
    public void SetColliderFixtureId(EntityUid uid, string? value, bool updateFixture = true, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref nyarlathotepHorizon))
            return;

        var oldValue = nyarlathotepHorizon.ColliderFixtureId;
        if (value == oldValue)
            return;

        nyarlathotepHorizon.ColliderFixtureId = value;
        Dirty(uid, nyarlathotepHorizon);
        if (updateFixture)
            UpdateNyarlathotepHorizonFixture(uid, nyarlathotepHorizon: nyarlathotepHorizon);
    }

    /// <summary>
    /// Setter for <see cref="NyarlathotepHorizonComponent.HorizonFixtureId"/>
    /// May also update the fixture associated with the Nyarlathotep horizon.
    /// </summary>
    /// <param name="uid">The uid of the Nyarlathotep horizon with the fixture ID to change.</param>
    /// <param name="value">The new fixture ID to associate the Nyarlathotep horizon with.</param>
    /// <param name="updateFixture">Whether to update the associated fixture upon changing the Nyarlathotep horizon.</param>
    /// <param name="nyarlathotepHorizon">The state of the Nyarlathotep horizon with the fixture ID to change.</param>
    public void SetConsumerFixtureId(EntityUid uid, string? value, bool updateFixture = true, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref nyarlathotepHorizon))
            return;

        var oldValue = nyarlathotepHorizon.ConsumerFixtureId;
        if (value == oldValue)
            return;

        nyarlathotepHorizon.ConsumerFixtureId = value;
        Dirty(uid, nyarlathotepHorizon);
        if (updateFixture)
            UpdateNyarlathotepHorizonFixture(uid, nyarlathotepHorizon: nyarlathotepHorizon);
    }

    /// <summary>
    /// Updates the state of the fixture associated with the Nyarlathotep horizon.
    /// </summary>
    /// <param name="uid">The uid of the Nyarlathotep horizon associated with the fixture to update.</param>
    /// <param name="fixtures">The fixture manager component containing the fixture to update.</param>
    /// <param name="nyarlathotepHorizon">The state of the Nyarlathotep horizon associated with the fixture to update.</param>
    public void UpdateNyarlathotepHorizonFixture(EntityUid uid, FixturesComponent? fixtures = null, NyarlathotepHorizonComponent? nyarlathotepHorizon = null)
    {
        if (!Resolve(uid, ref nyarlathotepHorizon))
            return;

        var consumerId = nyarlathotepHorizon.ConsumerFixtureId;
        var colliderId = nyarlathotepHorizon.ColliderFixtureId;
        if (consumerId == null || colliderId == null
        || !Resolve(uid, ref fixtures, logMissing: false))
            return;

        var consumer = _fixtures.GetFixtureOrNull(uid, consumerId, fixtures);
        if (consumer != null)
        {
            _physics.SetRadius(uid, consumerId, consumer, consumer.Shape, nyarlathotepHorizon.Radius, fixtures);
            _physics.SetHard(uid, consumer, false, fixtures);
        }

        var collider = _fixtures.GetFixtureOrNull(uid, colliderId, fixtures);
        if (collider != null)
        {
            _physics.SetRadius(uid, colliderId, collider, collider.Shape, nyarlathotepHorizon.Radius, fixtures);
            _physics.SetHard(uid, collider, false, fixtures);
        }

        EntityManager.Dirty(uid, fixtures);
    }

    #endregion Getters/Setters


    #region EventHandlers

    /// <summary>
    /// Syncs the state of the fixture associated with the Nyarlathotep horizon upon startup.
    /// </summary>
    /// <param name="comp">An entity that has just received a Nyarlathotep horizon component with a Nyarlathotep horizon component.</param>
    /// <param name="args">The Nyarlathotep arguments.</param>
    private void OnNyarlathotepHorizonStartup(Entity<NyarlathotepHorizonComponent> comp, ref ComponentStartup args)
    {
        UpdateNyarlathotepHorizonFixture(comp.Owner, nyarlathotepHorizon: comp.Comp);
    }

    /// <summary>
    /// Prevents the Nyarlathotep horizon from colliding with anything it cannot consume.
    /// Most notably map grids and ghosts.
    /// Also makes Nyarlathotep's horizons not swallow cult members.
    /// </summary>
    /// <param name="comp">An entity that has just received a Nyarlathotep horizon component with a Nyarlathotep horizon component.</param>
    /// <param name="args">The Nyarlathotep arguments.</param>
    private void OnPreventCollide(Entity<NyarlathotepHorizonComponent> comp, ref PreventCollideEvent args)
    {
        if (!args.Cancelled)
            PreventCollide(comp, ref args);
    }

    /// <summary>
    /// The actual, functional part of SharedNyarlathotepHorizonSystem.OnPreventCollide.
    /// The return value allows for overrides to early return if the base successfully handles collision prevention.
    /// </summary>
    /// <param name="comp">An entity that has just received a Nyarlathotep horizon component with a Nyarlathotep horizon component.</param>
    /// <param name="args">The Nyarlathotep arguments.</param>
    /// <returns>A bool indicating whether the collision prevention has been handled.</returns>
    protected virtual bool PreventCollide(Entity<NyarlathotepHorizonComponent> comp, ref PreventCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        if (HasComp<MapGridComponent>(otherUid) ||
            HasComp<GhostComponent>(otherUid))
        {
            args.Cancelled = true;
            return true;
        }

        return false;
    }

    #endregion EventHandlers
}
