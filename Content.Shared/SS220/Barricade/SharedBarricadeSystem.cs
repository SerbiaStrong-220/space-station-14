
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.Barricade;

public abstract partial class SharedBarricadeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarricadeComponent, PreventCollideEvent>(OnPreventCollide);

        SubscribeLocalEvent<PassBarricadeComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<PassBarricadeComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnPreventCollide(Entity<BarricadeComponent> entity, ref PreventCollideEvent args)
    {
        if (!TryComp<ProjectileComponent>(args.OtherEntity, out var projectile))
            return;

        CalculateChance(entity, (args.OtherEntity, projectile));
        if (TryComp<PassBarricadeComponent>(args.OtherEntity, out var passBarricade) &&
            passBarricade.CollideBarricades.TryGetValue(entity.Owner, out var isHit) &&
            !isHit)
            args.Cancelled = true;
    }

    private void OnProjectileHit(Entity<PassBarricadeComponent> entity, ref ProjectileHitEvent args)
    {
        entity.Comp.CollideBarricades.Clear();
    }

    private void OnEndCollide(Entity<PassBarricadeComponent> entity, ref EndCollideEvent args)
    {
        if (HasComp<BarricadeComponent>(args.OtherEntity))
            entity.Comp.CollideBarricades.Remove(args.OtherEntity);
    }

    protected virtual void CalculateChance(Entity<BarricadeComponent> entity, Entity<ProjectileComponent> projEnt)
    {

    }
}
