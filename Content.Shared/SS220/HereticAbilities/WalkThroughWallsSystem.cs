using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.HereticAbilities;

/// <summary>
/// This handles...
/// </summary>
public sealed class WalkThroughWallsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WalkThroughWallsComponent, ComponentInit>(OnCompInit);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<WalkThroughWallsComponent>();

        while (query.MoveNext(out var target, out var walkComp))
        {
            if (walkComp.IsInfinity)
                continue;

            if (!TryComp<FixturesComponent>(target, out var fixtures))
                continue;

            if (!fixtures.Fixtures.TryGetValue("fix1", out var fixture))
                continue;

            if (walkComp.Duration <= 0)
            {
                if (walkComp.PreviousGroupLayer != null)
                    _physics.SetCollisionLayer(target, "fix1", fixture, walkComp.PreviousGroupLayer.Value);

                if (walkComp.PreviousGroupMask != null)
                    _physics.SetCollisionMask(target, "fix1", fixture, walkComp.PreviousGroupMask.Value);

                walkComp.IsWalked = false;
                RemCompDeferred<WalkThroughWallsComponent>(target);
                continue;
            }

            if (walkComp.IsWalked)
            {
                walkComp.Duration -= frameTime;
                continue;
            }

            _physics.SetCollisionLayer(target, "fix1", fixture, walkComp.BulletImpassable);
            _physics.SetCollisionMask(target, "fix1", fixture, walkComp.WallsLayer);
            walkComp.IsWalked = true;
        }
    }

    private void OnCompInit(Entity<WalkThroughWallsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<FixturesComponent>(ent.Owner, out var fixtures))
            return;

        if (!fixtures.Fixtures.TryGetValue("fix1", out var fixture))
            return;

        if (!ent.Comp.IsInfinity)
        {
            ent.Comp.PreviousGroupLayer = fixture.CollisionLayer;
            ent.Comp.PreviousGroupMask = fixture.CollisionMask;
            return;
        }

        _physics.SetCollisionLayer(ent.Owner, "fix1", fixture, ent.Comp.BulletImpassable);
        _physics.SetCollisionMask(ent.Owner, "fix1", fixture, ent.Comp.WallsLayer);
    }
}
