// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Sprite;
using Content.Shared.Sprite;
using Content.Shared.SS220.RandomSpriteFixture;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.Server.SS220.RandomSpriteFixture;

public sealed partial class RandomSpriteFixtureSystem : EntitySystem
{
    [Dependency] private FixtureSystem _fixture = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomSpriteFixtureComponent, MapInitEvent>(OnMapInit, after: [typeof(RandomSpriteSystem)]);
    }

    private void OnMapInit(Entity<RandomSpriteFixtureComponent> ent, ref MapInitEvent args)
    {
        foreach (var (state, _) in Comp<RandomSpriteComponent>(ent).Selected.Values)
        {
            if (!ent.Comp.Bounds.TryGetValue(state, out var bounds))
                continue;

            SetBounds(ent, bounds);
            return;
        }
    }

    private void SetBounds(Entity<RandomSpriteFixtureComponent> ent, Box2 bounds)
    {
        var id = ent.Comp.FixtureId;
        var old = _fixture.GetFixtureOrNull(ent, id);
        if (old == null)
            return;

        var shape = new PolygonShape();
        shape.SetAsBox(bounds);

        _fixture.DestroyFixture(ent, id, old, false);
        _fixture.TryCreateFixture(
            ent,
            shape,
            id,
            old.Density,
            old.Hard,
            old.CollisionLayer,
            old.CollisionMask,
            old.Friction,
            old.Restitution);
    }
}
