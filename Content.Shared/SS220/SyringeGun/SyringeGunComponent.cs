// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SyringeGun;

[RegisterComponent, NetworkedComponent]
public sealed partial class SyringeGunComponent : Component
{
    /// <summary>
    /// Name of the container with the syringe
    /// </summary>
    [DataField("container", required: true)]
    public string? SyringeContainerId;

    /// <summary>
    /// Solution to inject from.
    /// </summary>
    [DataField("solution", required: true)]
    public string SolutionType = string.Empty;

    /// <summary>
    /// Speed of the projectile fired
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 20f;

    /// <summary>
    /// How long it takes to remove the embedded object.
    /// </summary>
    [DataField]
    public float SyringeRemovalTime = 1f;

    /// <summary>
    /// Projectile damage
    /// </summary>
    [DataField]
    public DamageSpecifier DamageOnHit = default!;

    /// <summary>
    /// Does the projectile ignore armor resistance.
    /// </summary>
    [DataField]
    public bool IgnoreResistances = false;

    /// <summary>
    /// Does the projectile ignore hardsuits or not.
    /// </summary>
    [DataField]
    public bool PierceArmor = true;

    /// <summary>
    /// Whether this will inject through armour vest or not.
    /// </summary>
    [DataField]
    public bool PierceArmorVest = true;

    [DataField]
    public Angle SyringeThrowAngle = Angle.Zero;
}
