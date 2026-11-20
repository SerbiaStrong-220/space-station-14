// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.Weapons.Ranged.Upgrades;

/// <summary>
/// Gun upgrade that absorbs an EMP hitting the gun's power cell, then needs <see cref="Cooldown"/> to recover.
/// A second EMP during the cooldown burns the fuse and drains the cell.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(GunUpgradeModulesSystem))]
public sealed partial class GunUpgradeEmpFuseComponent : Component
{
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ReadyTime;
}
