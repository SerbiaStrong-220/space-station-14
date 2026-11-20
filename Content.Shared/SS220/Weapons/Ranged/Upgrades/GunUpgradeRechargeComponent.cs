// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.Weapons.Ranged.Upgrades;

/// <summary>
/// Gun upgrade that passively recharges the gun's battery.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(GunUpgradeModulesSystem))]
public sealed partial class GunUpgradeRechargeComponent : Component
{
    /// <summary>
    /// Charge added to the battery per second.
    /// </summary>
    [DataField]
    public float ChargeRate = 2.5f;

    /// <summary>
    /// How long recharging stops after each shot.
    /// </summary>
    [DataField]
    public TimeSpan PauseOnShot = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? PausedUntil;
}
