using Content.Shared.SS220.Forcefield.Components;
using Content.Shared.SS220.Forcefield.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System.Linq;

namespace Content.Server.SS220.Forcefield.Systems;

public sealed partial class ForcefieldSystem : SharedForcefieldSystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcefieldComponent, MapInitEvent>(OnMapInit);
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

    public override void RefreshFixtures(Entity<ForcefieldComponent?, FixturesComponent?> entity)
    {
        base.RefreshFixtures(entity);

        if (!Resolve(entity, ref entity.Comp1) ||
            !Resolve(entity, ref entity.Comp2))
            return;

        var forcefield = entity.Comp1;
        var fixtures = entity.Comp2;

        foreach (var fixture in fixtures.Fixtures)
            _fixture.DestroyFixture(entity, fixture.Key, false, manager: fixtures);

        var shapes = forcefield.Figure.GetShapes();
        for (var i = 0; i < shapes.Count(); i++)
        {
            var shape = shapes.ElementAt(i);
            _fixture.TryCreateFixture(
            entity,
            shape,
            $"shape{i + 1}",
            density: forcefield.Destiny,
            collisionLayer: forcefield.CollisionLayer,
            collisionMask: forcefield.CollisionMask,
            manager: fixtures,
            updates: false
            );
        }

        _fixture.FixtureUpdate(entity, manager: fixtures);
    }
}
