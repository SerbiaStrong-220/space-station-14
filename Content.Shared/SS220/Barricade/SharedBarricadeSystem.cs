// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Projectiles;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.Barricade;

public abstract partial class SharedBarricadeSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarricadeComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<BarricadeComponent, HitscanAttempt>(OnHitscanAttempt);

        SubscribeLocalEvent<PassBarricadeComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<PassBarricadeComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<PassBarricadeComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnPreventCollide(Entity<BarricadeComponent> entity, ref PreventCollideEvent args)
    {
        if (_entityWhitelist.IsWhitelistPass(entity.Comp.Whitelist, args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<ProjectileComponent>(args.OtherEntity, out var projectile) &&
            ProjectileTryPassBarricade(entity, (args.OtherEntity, projectile)))
        {
            args.Cancelled = true;
            return;
        }
    }

    private void OnHitscanAttempt(Entity<BarricadeComponent> entity, ref HitscanAttempt args)
    {
        if (HitscanTryPassBarricade(entity, args.User))
            args.Cancelled = true;
    }

    private void OnLand(Entity<PassBarricadeComponent> entity, ref LandEvent args)
    {
        entity.Comp.CollideBarricades.Clear();
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

    protected virtual bool HitscanTryPassBarricade(Entity<BarricadeComponent> entity, EntityUid source, TransformComponent? sourceXform = null)
    {
        return false;
    }

    protected virtual bool ProjectileTryPassBarricade(Entity<BarricadeComponent> entity, Entity<ProjectileComponent> projEnt)
    {
        if (TryComp<PassBarricadeComponent>(projEnt.Owner, out var passBarricade) &&
            passBarricade.CollideBarricades.TryGetValue(entity.Owner, out var isPass))
            return isPass;

        return false;
    }
}
