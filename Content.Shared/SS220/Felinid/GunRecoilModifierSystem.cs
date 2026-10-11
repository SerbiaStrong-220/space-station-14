using System.Numerics;
using Content.Shared.Gravity;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.SS220.Felinid;

/// <summary>
/// Throws a shooter that opts into ground recoil away from the shot by the weapon's
/// <see cref="GunComponent.FelinidRecoil"/> tiles. Weightless shooters keep the regular impulse push.
/// </summary>
public abstract partial class SharedGunRecoilModifierSystem : EntitySystem
{
    private const float RecoilThrowSpeed = 2f;

    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunRecoilModifierComponent, ShooterImpulseEvent>(OnShooterImpulse);
    }

    private void OnShooterImpulse(Entity<GunRecoilModifierComponent> ent, ref ShooterImpulseEvent args)
    {
        var shot = _transform.ToMapCoordinates(args.To).Position - _transform.ToMapCoordinates(args.From).Position;
        if (shot == Vector2.Zero ||
            _gravity.IsWeightless(ent.Owner) ||
            !_hands.TryGetActiveItem(ent.Owner, out var activeItem) ||
            !TryComp<GunComponent>(activeItem, out var gun) ||
            gun.FelinidRecoil <= 0f)
        {
            return;
        }

        // A slow throw keeps the felinid airborne for nearly the whole distance, so floor friction barely changes it.
        _throwing.TryThrow(
            ent.Owner,
            -shot.Normalized() * gun.FelinidRecoil,
            RecoilThrowSpeed,
            recoil: false,
            animated: false,
            playSound: false,
            doSpin: false);

        var recoil = new GunRecoilImpulseEvent(gun.FelinidRecoilKnockdownChance, gun.FelinidRecoilKnockdownTime);
        RaiseLocalEvent(ent.Owner, ref recoil);
    }
}
