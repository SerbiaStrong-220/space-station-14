// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.FCB.Mech.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.FCB.Mech.Equipment.Components;

/// <summary>
/// A piece of equipment that can be installed into <see cref="AltMechComponent"/>
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AltMechEquipmentComponent : Component
{
    /// <summary>
    /// How long does it take to install this piece of equipment
    /// </summary>
    [DataField("installDuration")] public float InstallDuration = 5;

    /// <summary>
    /// The mech that the equipment is inside of.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? EquipmentOwner;

    [DataField]
    public EntityUid? EquipmentAbilityAction;

    /// <summary>
    /// How much does this equipment weight
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 OwnMass = 0;

    /// <summary>
    /// Prototype of action this equpment provides
    /// </summary>
    [DataField]
    public EntProtoId EquipmentAbilityActionName;

    /// <summary>
    /// How much space this equipment takes
    /// </summary>
    [DataField("size")]
    public int EqipmentSize;
}
