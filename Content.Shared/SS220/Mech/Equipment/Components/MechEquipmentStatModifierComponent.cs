// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Mech.Equipment.Components;

[RegisterComponent]
[NetworkedComponent]

public sealed partial class MechEquipmentStatModifierComponent : Component
{
    /// <summary>
    /// The maximum amount of damage the mech can take.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxIntegrityDelta = 0;

    /// <summary>
    /// How much does core part weight
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 OwnMassDelta = 0;

    /// <summary>
    /// Maximal mass of an arm that can be installed into this mech frame
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaximalArmMassDelta = 0;
}
