using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Mech.Components;

/// <summary>
/// Chassis(a.k.a.legs) of the mech
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechChassisComponent : Component
{
    /// <summary>
    /// How much "health" the mech has left.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Integrity;

    /// <summary>
    /// The maximum amount of damage the mech can take.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxIntegrity = 250;

    /// <summary>
    /// How much does core part weight
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 OwnMass = 0;

    /// <summary>
    /// How much the mech(core+all attached parts and equipment) weights
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 OverallMass = 0;

    /// <summary>
    /// How much the mech can carry
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaximalMass = 0;

    /// <summary>
    /// The slot the battery is stored in.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BatterySlot = default!;

    /// <summary>
    /// Whether the mech has been destroyed and is no longer pilotable.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Broken = false;

    /// <summary>
    /// The maximum amount of equipment items that can be installed in the mech
    /// </summary>
    [DataField("maxEquipmentAmount"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxEquipmentAmount = 3;

    /// <summary>
    /// A whitelist for inserting equipment items.
    /// </summary>
    [DataField]
    public EntityWhitelist? EquipmentWhitelist;

    /// <summary>
    /// Movement speed this chassis provides
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public MovementSpeedModifier BaseMovementSpeed;

    /// <summary>
    /// A container for storing the equipment entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Container EquipmentContainer = default!;

    [ViewVariables]
    public readonly string EquipmentContainerId = "mech-equipment-container";

    /// <summary>
    /// The equipment that the mech initially has when it spawns.
    /// Good for things like nukie mechs that start with guns.
    /// </summary>
    [DataField]
    public List<EntProtoId> StartingEquipment = new();

}
