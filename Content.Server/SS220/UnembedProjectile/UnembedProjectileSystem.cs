using Content.Server.Projectiles;
using Content.Shared.Projectiles;

namespace Content.Server.SS220.UnembedProjectile;

public sealed partial class UnembedProjectileSystem : EntitySystem
{
    [Dependency] private readonly ProjectileSystem _projectile = default!;

    public int UnembedChildren(EntityUid uid)
    {
        int counter = 0;

        if (TryComp(uid, out TransformComponent? transform))
        {
            var @enum = transform.ChildEnumerator;

            while (@enum.MoveNext(out var child))
                if (TryComp<EmbeddableProjectileComponent>(child, out var embeddableComp))
                    if (embeddableComp.EmbeddedIntoUid == uid)
                    {
                        _projectile.UnEmbed(child, embeddableComp);
                        counter++;
                    }
        }

        return counter;
    }
}
