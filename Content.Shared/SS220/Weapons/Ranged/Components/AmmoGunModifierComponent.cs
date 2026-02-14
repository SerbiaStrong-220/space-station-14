using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.Weapons.Ranged.Components;

/// <summary>
/// Modifies GunComponent fields values when this ammo is shot once.
/// Applies on one and only shot of this ammo.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AmmoGunModifierComponent : Component
{
    /// <summary>
    /// Alters the sound to use when the gun is fired.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundGunshotAlt;

    /// <summary>
    /// Alters the value for the minimum angle allowed for <see cref="CurrentAngle"/>
    /// </summary>
    [DataField]
    public Angle? MinAngleDelta;

    /// <summary>
    /// Alters the value for the maximum angle allowed for <see cref="CurrentAngle"/>
    /// </summary>
    [DataField]
    public Angle? MaxAngleDelta;

    /// <summary>
    /// Alters how much the spread increases once the gun fired.
    /// </summary>
    [DataField]
    public Angle? AngleIncreaseDelta;

    /// <summary>
    /// Alters how much the <see cref="CurrentAngle"/> decreases per second.
    /// </summary>
    [DataField]
    public Angle? AngleDecayDelta;

    /// <summary>
    /// Alters how many times gun shoots per second.
    /// </summary>
    [DataField]
    public float? FireRateDelta;

    /// <summary>
    /// Alters how many shots to fire per burst.
    /// </summary>
    [DataField]
    public int? ShotsPerBurstDelta;

    /// <summary>
    /// Alters how fast the projectile moves.
    /// </summary>
    [DataField]
    public float? ProjectileSpeedDelta;

    /// <summary>
    /// Alters a scalar value applied to the vector governing camera recoil.
    /// If reaches 0, there will be no camera recoil.
    /// </summary>
    [DataField]
    public float? CameraRecoilScalarDelta;
}
