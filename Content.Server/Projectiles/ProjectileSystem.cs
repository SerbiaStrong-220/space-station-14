using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Effects;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.SS220.AltArmor.Components;
using Content.Shared.SS220.Projectiles.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.ProjectileSpent || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        var target = args.OtherEntity;
        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        //SS220 shield rework begin
        var blockattemptEv = new ProjectileBlockAttemptEvent(uid, component.Damage);
        RaiseLocalEvent(target, ref blockattemptEv);
        if (blockattemptEv.Cancelled)
        {
            if (TryGetNetEntity(target, out var netTarget))
            {
                var blockedComp = EnsureComp<BlockedProjectileComponent>(uid);
                blockedComp.BlockerEntity = netTarget;
                Dirty(uid, blockedComp);
            }

            SetShooter(uid, component, target);
            QueueDel(uid);

            if (blockattemptEv.hitMarkColor != null)
                _color.RaiseEffect((Color)blockattemptEv.hitMarkColor, new List<EntityUid>() { target }, Filter.Pvs(target, entityManager: EntityManager));

            return;
        }
        //SS220 shield rework end

        var ev = new ProjectileHitEvent(component.Damage * _damageableSystem.UniversalProjectileDamageModifier, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var damageRequired = _destructibleSystem.DestroyedAt(target);
        if (TryComp<DamageableComponent>(target, out var damageableComponent))
        {
            damageRequired -= _damageableSystem.GetTotalDamage((target, damageableComponent));
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }
        var deleted = Deleted(target);

        if (_damageableSystem.TryChangeDamage((target, damageableComponent), ev.Damage, out var damage, component.IgnoreResistances, origin: component.Shooter) && Exists(component.Shooter))
        {
            //SS220 weapon overhaul begin
            if (damage != null)
                component.Damage = damage;
            //SS220 weapon overhaul end

            if (!deleted)
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Medium,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {damage:damage} damage");

            component.ProjectileSpent = !TryPenetrate((uid, component), damage, (target, damageableComponent)); //SS220 structure penetration overhaul
        }
        else
        {
            component.ProjectileSpent = true;
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, damage, component.SoundHit, component.ForceSound);

            if (!args.OurBody.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, args.OurBody.LinearVelocity.Normalized());
        }

        if (component.DeleteOnCollide && component.ProjectileSpent)
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
        {
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
        }
    }

    //SS220 structure penetration overhaul begin
    private bool TryPenetrate(Entity<ProjectileComponent> projectile, DamageSpecifier? damage, Entity<DamageableComponent?> target)
    {
        if (damage == null)
            return false;

        if (projectile.Comp.PenetrationDamageTypeRequirement == null || target.Comp == null)
            return false;

        var stopPenetration = false;
        foreach (var requiredDamageType in projectile.Comp.PenetrationDamageTypeRequirement)
        {
            if (!damage.DamageDict.Keys.Contains(requiredDamageType))
            {
                stopPenetration = true;
                break;
            }
            FixedPoint2 targetThreshold = 0f;

            targetThreshold = target.Comp.PiercingThreshold.Float();

            if (TryComp<AltArmorComponent>(target, out var armorComp) && armorComp.TresholdDict.TryGetValue(requiredDamageType, out var value))
                targetThreshold += value;

            if (projectile.Comp.Damage[requiredDamageType] + projectile.Comp.Damage.ArmourPiercing < targetThreshold)
            {
                stopPenetration = true;
                return false;
            }

            var resultThreshold = FixedPoint2.Clamp(targetThreshold - projectile.Comp.Damage.ArmourPiercing, FixedPoint2.Zero, FixedPoint2.Abs(targetThreshold + projectile.Comp.Damage.ArmourPiercing));

            var leftToRemove = FixedPoint2.Max(FixedPoint2.Zero, targetThreshold - projectile.Comp.Damage.ArmourPiercing);

            projectile.Comp.Damage.ArmourPiercing = FixedPoint2.Max(FixedPoint2.Zero, projectile.Comp.Damage.ArmourPiercing - targetThreshold);

            projectile.Comp.Damage.DamageDict[requiredDamageType] = FixedPoint2.Max(projectile.Comp.Damage.DamageDict[requiredDamageType] - leftToRemove, FixedPoint2.Zero);
        }
        if (stopPenetration)
            return false;

        return true;
    }
    //SS220 structure penetration overhaul end
}
