using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Effects;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.SS220.AltArmor.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; //SS220 structure penetration rework
    [Dependency] private readonly SharedTransformSystem _transform = default!; //SS220 shield rework

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
        var projectileAngle = _transform.GetWorldRotation(uid);
        var blockattemptEv = new ProjectileBlockAttemptEvent(uid, component, false, component.Damage, (projectileAngle + new Angle(Math.PI)).Reduced());
        RaiseLocalEvent(target, ref blockattemptEv);
        if (blockattemptEv.CancelledHit)
        {
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
            damageRequired -= damageableComponent.TotalDamage;
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }
        var modifiedDamage = _damageableSystem.TryChangeDamage(target, ev.Damage, component.IgnoreResistances, damageable: damageableComponent, origin: component.Shooter);

        if(modifiedDamage != null) //SS220 weapon overhaul
            component.Damage = modifiedDamage; //SS220 weapon overhaul

        var deleted = Deleted(target);

        if (modifiedDamage is not null && Exists(component.Shooter))
        {
            if (modifiedDamage.AnyPositive() && !deleted)
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Medium,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {modifiedDamage.GetTotal():damage} damage");
        }

        //SS220 structure penetration overhaul begin
        if (modifiedDamage is not null)// The idea is to make every weapon theorethically able to penetrate and use ArmourPiercing and the damage itself for it's logics
        {
            // If a damage type is required, stop the bullet if the hit entity doesn't have that type.
            if (component.PenetrationDamageTypeRequirement != null && damageableComponent != null)//SS220 structure penetration overhaul
            {
                var stopPenetration = false;
                foreach (var requiredDamageType in component.PenetrationDamageTypeRequirement)
                {
                    if (!modifiedDamage.DamageDict.Keys.Contains(requiredDamageType))
                    {
                        stopPenetration = true;
                        break;
                    }
                    float targetThreshold = 0f;

                    if (damageableComponent != null)
                        targetThreshold = damageableComponent.PiercingThreshold.Float();

                    if (TryComp<AltArmorComponent>(target, out var armorComp) && armorComp.TresholdDict.ContainsKey(requiredDamageType))
                        targetThreshold += armorComp.TresholdDict[requiredDamageType].Float();

                    if (component.Damage[requiredDamageType] + component.Damage.ArmourPiercing < targetThreshold)
                        stopPenetration = true;

                    var resultThreshold = Math.Clamp((targetThreshold - component.Damage.ArmourPiercing).Float(), 0f, Math.Abs(targetThreshold + component.Damage.ArmourPiercing.Float()));

                    component.Damage.ArmourPiercing = component.Damage.ArmourPiercing.Float() - targetThreshold;

                    component.Damage.DamageDict[requiredDamageType] = Math.Clamp((component.Damage.DamageDict[requiredDamageType] - resultThreshold).Float(), 0f, (component.Damage.DamageDict[requiredDamageType] + resultThreshold).Float());

                    if (component.Damage[requiredDamageType] < resultThreshold)
                        stopPenetration = true;
                }
                if (stopPenetration)
                {
                    component.ProjectileSpent = true;
                    SetShooter(uid, component, target);
                    QueueDel(uid);
                }
            }
        }
        //SS220 structure penetration overhaul end
        else
        {
            component.ProjectileSpent = true;
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, modifiedDamage, component.SoundHit, component.ForceSound);

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
}
