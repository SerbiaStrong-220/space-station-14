using Content.Server.Administration.Logs;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Damage;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Damage.Systems;

public sealed class DamageOtherOnHitSystem : SharedDamageOtherOnHitSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly Shared.Damage.Systems.DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!; // SS220 shield rework

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOtherOnHitComponent, ThrowDoHitEvent>(OnDoHit);
    }

    private void OnDoHit(EntityUid uid, DamageOtherOnHitComponent component, ThrowDoHitEvent args)
    {
        if (TerminatingOrDeleted(args.Target))
            return;

        // SS220-add-miss-chance-?-begin
        var hitEv = new GetDamageOtherOnHitEvent(GetNetEntity(uid), GetNetEntity(args.Target), component.Damage, component.IgnoreResistances);
        RaiseLocalEvent(uid, hitEv, broadcast: true);

        if (hitEv.Handled)
            return;
        // SS220-add-miss-chance-?-end

        //SS220 shield rework begin
        var blockEv = new ThrowableProjectileBlockAttemptEvent(component.Damage, _transformSystem.GetWorldRotation(uid) + new Angle(Math.PI));

        RaiseLocalEvent(args.Target, ref blockEv);
        if (blockEv.CancelledHit)
        {
            _color.RaiseEffect(Color.Red, [args.Target], Filter.Pvs(args.Target, entityManager: EntityManager));
            return;
        }
        //SS220 shield rework end

        var dmg = _damageable.TryChangeDamage(args.Target, component.Damage * _damageable.UniversalThrownDamageModifier, component.IgnoreResistances, origin: args.Component.Thrower);
        var dmg = _damageable.ChangeDamage(args.Target, component.Damage * _damageable.UniversalThrownDamageModifier, component.IgnoreResistances, origin: args.Component.Thrower);

        // Log damage only for mobs. Useful for when people throw spears at each other, but also avoids log-spam when explosions send glass shards flying.
        if (HasComp<MobStateComponent>(args.Target))
            _adminLogger.Add(LogType.ThrowHit, $"{ToPrettyString(args.Target):target} received {dmg.GetTotal():damage} damage from collision");

        if (!dmg.Empty)
        {
            _color.RaiseEffect(Color.Red, [args.Target], Filter.Pvs(args.Target, entityManager: EntityManager));
        }

        _guns.PlayImpactSound(args.Target, dmg, null, false);
        if (TryComp<PhysicsComponent>(uid, out var body) && body.LinearVelocity.LengthSquared() > 0f)
        {
            var direction = body.LinearVelocity.Normalized();
            _sharedCameraRecoil.KickCamera(args.Target, direction);
        }
    }
}
