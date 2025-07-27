// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.SS220.Forcefield.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System.Linq;

namespace Content.Server.SS220.Forcefield.Systems;

public sealed partial class ForcefieldSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcefieldComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ForcefieldComponent, DamageChangedEvent>(OnDamageChange);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ForcefieldComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Figure.Dirty)
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

    public void RefreshFigure(Entity<ForcefieldComponent> entity)
    {
        entity.Comp.Figure.Refresh();
        Dirty(entity);

        if (TryComp<FixturesComponent>(entity, out var fixtures))
            RefreshFixtures((entity, entity.Comp, fixtures));
    }

    public void RefreshFixtures(Entity<ForcefieldComponent?, FixturesComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp1) ||
            !Resolve(entity, ref entity.Comp2))
            return;

        var forcefield = entity.Comp1;
        var fixtures = entity.Comp2;

        foreach (var fixture in fixtures.Fixtures)
            _fixture.DestroyFixture(entity, fixture.Key, false, manager: fixtures);

        var shapes = forcefield.Figure.GetShapes();
        var density = forcefield.Density / shapes.Count();
        for (var i = 0; i < shapes.Count(); i++)
        {
            var shape = shapes.ElementAt(i);
            _fixture.TryCreateFixture(
            entity,
            shape,
            $"shape{i}",
            density: density,
            collisionLayer: forcefield.CollisionLayer,
            collisionMask: forcefield.CollisionMask,
            manager: fixtures,
            updates: false
            );
        }

        _physics.SetCanCollide(entity, true);
        _fixture.FixtureUpdate(entity, manager: fixtures);
    }
}

public sealed class ForcefieldDamageChangedEvent(Entity<ForcefieldComponent> forcefield, DamageChangedEvent ev) : EntityEventArgs
{
    public Entity<ForcefieldComponent> Forcefield = forcefield;
    public DamageChangedEvent Event = ev;
}
