using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Damage.Systems;

public sealed class RequireProjectileTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RequireProjectileTargetComponent, PreventCollideEvent>(PreventCollide);
        SubscribeLocalEvent<RequireProjectileTargetComponent, StoodEvent>(StandingBulletHit);
        SubscribeLocalEvent<RequireProjectileTargetComponent, DownedEvent>(LayingBulletPass);
    }

    private void PreventCollide(Entity<RequireProjectileTargetComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
          return;

        if (!ent.Comp.Active)
            return;

        var other = args.OtherEntity;
        //SS220 weapon overhaul begin
        if (TryComp(other, out ProjectileComponent? projectile))
        {
            if (TryComp<GunAimableComponent>(projectile.Weapon, out var aimComp) && aimComp.IsAimed)
            {
                if (TryComp<MobStateComponent>(ent.Owner, out var statesComp) && (statesComp.CurrentState == Mobs.MobState.Alive))
                    return;
            }

            if (CompOrNull<TargetedProjectileComponent>(other)?.Target != ent)
            {
                // Prevents shooting out of while inside of crates
                var shooter = projectile.Shooter;
                if (!shooter.HasValue)
                    return;

                // ProjectileGrenades delete the entity that's shooting the projectile,
                // so it's impossible to check if the entity is in a container
                if (TerminatingOrDeleted(shooter.Value))
                    return;

                if (!_container.IsEntityOrParentInContainer(shooter.Value))
                    args.Cancelled = true;
            }
        }
        //SS220 weapon overhaul end
    }

    //SS220 weapon overhaul begin
    public bool PreventHitscan(Entity<RequireProjectileTargetComponent> ent, EntityUid gunUid)
    {
        if (!ent.Comp.Active)
            return false;

        if (TryComp<GunAimableComponent>(gunUid, out var aimComp) && aimComp.IsAimed)
            if (TryComp<MobStateComponent>(ent.Owner, out var statesComp) && (statesComp.CurrentState == Mobs.MobState.Alive))
                return false;

        return true;
    }
    //SS220 weapon overhaul end

    private void SetActive(Entity<RequireProjectileTargetComponent> ent, bool value)
    {
        if (ent.Comp.Active == value)
            return;

        ent.Comp.Active = value;
        Dirty(ent);
    }

    private void StandingBulletHit(Entity<RequireProjectileTargetComponent> ent, ref StoodEvent args)
    {
        SetActive(ent, false);
    }

    private void LayingBulletPass(Entity<RequireProjectileTargetComponent> ent, ref DownedEvent args)
    {
        SetActive(ent, true);
    }
}
