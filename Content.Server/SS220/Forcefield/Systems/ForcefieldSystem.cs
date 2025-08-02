// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.SS220.Forcefield.Components;
using Content.Shared.SS220.Forcefield.Systems;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using System.Linq;
using Robust.Server.Audio;

namespace Content.Server.SS220.Forcefield.Systems;

public sealed partial class ForcefieldSystem : SharedForcefieldSystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private readonly Dictionary<EntityUid, List<ICommonSession>> _curPvsOverrides = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcefieldComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ForcefieldComponent, DamageChangedEvent>(OnDamageChange);
        SubscribeLocalEvent<ForcefieldComponent, MoveEvent>(OnMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ForcefieldComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdatePvsOverride((uid, comp));

            if (!comp.Params.Figure.Dirty)
                continue;

            RefreshFigure((uid, comp));
        }
    }

    private void OnMapInit(Entity<ForcefieldComponent> entity, ref MapInitEvent args)
    {
        RefreshFigure(entity);
    }

    private void OnDamageChange(Entity<ForcefieldComponent> entity, ref DamageChangedEvent args)
    {
        if (entity.Comp.FieldOwner is { } owner)
        {
            var ev = new ForcefieldDamageChangedEvent(entity, args);
            RaiseLocalEvent(GetEntity(owner), ev);
        }
    }

    private void OnMove(Entity<ForcefieldComponent> entity, ref MoveEvent args)
    {
        RefreshFigure(entity);
    }

    public void RefreshFigure(Entity<ForcefieldComponent> entity)
    {
        if (TerminatingOrDeleted(entity))
            return;

        entity.Comp.Params.Figure.OwnerRotation = Transform(entity).LocalRotation;
        entity.Comp.Params.Figure.Refresh();
        Dirty(entity);

        if (TryComp<FixturesComponent>(entity, out var fixtures))
            RefreshFixtures((entity, entity.Comp, fixtures));
    }

    public void RefreshFixtures(Entity<ForcefieldComponent?, FixturesComponent?> entity)
    {
        if (TerminatingOrDeleted(entity))
            return;

        if (!Resolve(entity, ref entity.Comp1) ||
            !Resolve(entity, ref entity.Comp2))
            return;

        var forcefield = entity.Comp1;
        var fixtures = entity.Comp2;

        foreach (var fixture in fixtures.Fixtures)
            _fixture.DestroyFixture(entity, fixture.Key, false, manager: fixtures);

        var shapes = forcefield.Params.Figure.GetShapes();
        var density = forcefield.Params.Density / shapes.Count();
        for (var i = 0; i < shapes.Count(); i++)
        {
            var shape = shapes.ElementAt(i);
            _fixture.TryCreateFixture(
            entity,
            shape,
            $"shape{i}",
            density: density,
            collisionLayer: forcefield.Params.CollisionLayer,
            collisionMask: forcefield.Params.CollisionMask,
            manager: fixtures,
            updates: false
            );
        }

        _physics.SetCanCollide(entity, true);
        _fixture.FixtureUpdate(entity, manager: fixtures);
    }

    private void UpdatePvsOverride(Entity<ForcefieldComponent> entity)
    {
        var pvsRange = _configurationManager.GetCVar(CVars.NetMaxUpdateRange);

        var curOverrides = _curPvsOverrides.GetOrNew(entity);
        var toRemove = curOverrides.ToList();
        var toAdd = new List<ICommonSession>();

        foreach (var session in _player.Sessions)
        {
            var attachedEnt = session.AttachedEntity;
            if (attachedEnt is null)
                continue;

            var entMapCoords = _transform.GetMapCoordinates(attachedEnt.Value);
            var forcefieldMapCoords = _transform.GetMapCoordinates(attachedEnt.Value);

            if (forcefieldMapCoords.MapId != entMapCoords.MapId)
                continue;

            var entLocalCoords = _transform.ToCoordinates(entity.Owner, entMapCoords);
            var closestPoint = entity.Comp.Params.Figure.GetClosestPoint(entLocalCoords.Position);
            if (closestPoint is null)
                continue;

            var distanse = (entLocalCoords.Position - closestPoint.Value).Length();
            if (distanse > pvsRange)
                continue;

            if (!toRemove.Remove(session))
                toAdd.Add(session);
        }

        foreach (var session in toRemove)
        {
            _pvsOverride.RemoveSessionOverride(entity, session);
            curOverrides.Remove(session);
        }

        foreach (var session in toAdd)
        {
            _pvsOverride.AddSessionOverride(entity, session);
            curOverrides.Add(session);
        }

        if (curOverrides.Count > 0)
            _curPvsOverrides.TryAdd(entity, curOverrides);
        else
            _curPvsOverrides.Remove(entity);
    }
}

public sealed class ForcefieldDamageChangedEvent(Entity<ForcefieldComponent> forcefield, DamageChangedEvent ev) : EntityEventArgs
{
    public Entity<ForcefieldComponent> Forcefield = forcefield;
    public DamageChangedEvent Event = ev;
}
