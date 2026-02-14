using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.SS220.Weapons.Ranged.Components;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed class AmmoGunModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    private void OnGunRefreshModifiers(EntityUid uid, GunComponent component, ref GunRefreshModifiersEvent args)
    {
        if (args.CurrentAmmoModifier == null)
            return;

        var modifier = args.CurrentAmmoModifier;

        if (modifier.SoundGunshotAlt != null)
            args.SoundGunshot = modifier.SoundGunshotAlt;
        if (modifier.MinAngleDelta != null)
            args.MinAngle += modifier.MinAngleDelta.Value;
        if (modifier.MaxAngleDelta != null)
            args.MaxAngle += modifier.MaxAngleDelta.Value;
        if (modifier.AngleIncreaseDelta != null)
            args.AngleIncrease += modifier.AngleIncreaseDelta.Value;
        if (modifier.AngleDecayDelta != null)
            args.AngleDecay += modifier.AngleDecayDelta.Value;
        if (modifier.FireRateDelta != null)
            args.FireRate += modifier.FireRateDelta.Value;
        if (modifier.ShotsPerBurstDelta != null)
            args.ShotsPerBurst += modifier.ShotsPerBurstDelta.Value;
        if (modifier.ProjectileSpeedDelta != null)
            args.ProjectileSpeed += modifier.ProjectileSpeedDelta.Value;
        if (modifier.CameraRecoilScalarDelta != null)
            args.CameraRecoilScalar += modifier.CameraRecoilScalarDelta.Value;
    }
}
