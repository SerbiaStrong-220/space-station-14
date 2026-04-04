using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows GunSystem to automatically fire while this component is enabled
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem)), AutoGenerateComponentState]
public sealed partial class AutoShootGunComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled;

    //ShuttleGuns_fix start (#3180)
    /// <summary>
    /// Shuttles canons field, make a true if you want the weapon to be able to start shooting when unanchored
    /// </summary>
    [DataField]
    public bool CanShootUnanchored = false;

    /// <summary>
    /// Shuttles canons field, make true if you want the weapon to not require power to start shooting
    /// </summary>
    [DataField]
    public bool RequiredPower = true;
    //SS220 ShuttleGuns_fix end (#3180)
}
