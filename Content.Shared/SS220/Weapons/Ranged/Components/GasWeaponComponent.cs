// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT

using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Weapons.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class GasWeaponComponent : Component
{
    public const string TankSlotId = "gas_tank";

    /// <summary>
    ///     Amount of moles to consume for each shot at any power.
    /// </summary>
    [DataField("gasType")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Gas GasType = Gas.Plasma;

    /// <summary>
    ///     Amount of moles to consume for each shot at any power.
    /// </summary>
    [DataField("gasUsage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float GasUsage = 0.142f;
}
