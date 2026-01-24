using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects.Effects.StatusEffects;

namespace Content.Shared.SS220.Mech.Components;

/// <summary>
/// A large, pilotable machine that has equipment that is
/// powered via an internal battery.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AltMechComponent : Component
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
    /// How much energy the mech has.
    /// Derived from the currently inserted battery.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Energy = 0;

    /// <summary>
    /// How much does core part weight
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 OwnMass = 0;

    /// <summary>
    /// How much the mech(core+all attached parts and equipment) weights
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 OverallMass = 0;

    /// <summary>
    /// How much the mech can carry
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaximalMass = 0;

    /// <summary>
    /// The maximum amount of energy the mech can have.
    /// Derived from the currently inserted battery.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxEnergy = 0;

    /// <summary>
    /// The slot the battery is stored in.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BatterySlot = default!;

    [ViewVariables]
    public readonly string BatterySlotId = "mech-battery-slot";

    /// <summary>
    /// Whether the mech has been destroyed and is no longer pilotable.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Broken = false;

    /// <summary>
    /// The slot the pilot is stored in.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot PilotSlot = default!;

    [ViewVariables]
    public readonly string PilotSlotId = "mech-pilot-slot";

    [AutoNetworkedField]
    public float? OverallBaseMovementSpeed = 1f;

    [AutoNetworkedField]
    public float? OverallMovementSpeedModifier = 1f;

    [AutoNetworkedField]
    public bool MaintenanceMode = true; //if the mech is not in the maintenance mode we cannot interact with its parts or equipment

    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, ContainerSlot> ContainerDict = new();

    //[ViewVariables(VVAccess.ReadWrite)]
    //public ContainerSlot HeadSlot = default!;

    //[ViewVariables]
    //public readonly string HeadSlotId = "mech-head-slot";

    //[ViewVariables(VVAccess.ReadWrite)]
    //public ContainerSlot LegsSlot = default!;

    //[ViewVariables]
    //public readonly string LegsSlotId = "mech-legs-slot";

    //[ViewVariables(VVAccess.ReadWrite)]
    //public ContainerSlot PowerGridSlot = default!;

    //[ViewVariables]
    //public readonly string PowerGridSlotId = "mech-power-slot";

    //[ViewVariables(VVAccess.ReadWrite)]
    //public ContainerSlot LeftArmSlot = default!;

    //[ViewVariables]
    //public readonly string LeftArmSlotId = "mech-left-arm-slot";

    //[ViewVariables(VVAccess.ReadWrite)]
    //public ContainerSlot RightArmSlot = default!;

    //[ViewVariables]
    //public readonly string RightArmSlotSlotId = "mech-right-arm-slot";

    [DataField]
    public EntityWhitelist? HeadWhitelist;

    [DataField]
    public EntityWhitelist? LegsWhitelist;

    [DataField]
    public EntityWhitelist? PowerGridWhitelist;

    [DataField]
    public EntityWhitelist? LeftArmWhitelist;

    [DataField]
    public EntityWhitelist? RightArmWhitelist;

    [DataField]
    public EntityWhitelist? PilotWhitelist;

    /// <summary>
    /// How long it takes to enter the mech.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EntryDelay = 3;

    /// <summary>
    /// How long it takes to pull *another person*
    /// outside of the mech. You can exit instantly yourself.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ExitDelay = 3;

    /// <summary>
    /// How long it takes to pull out the battery.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BatteryRemovalDelay = 2;

    /// <summary>
    /// Whether or not the mech is airtight.
    /// </summary>
    /// <remarks>
    /// This needs to be redone
    /// when mech internals are added
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Airtight;

    #region Action Prototypes
    [DataField]
    public EntProtoId MechCycleAction = "ActionMechCycleEquipment";
    [DataField]
    public EntProtoId MechUiAction = "ActionMechOpenUI";
    [DataField]
    public EntProtoId MechEjectAction = "ActionMechEject";
    [DataField]
    public EntProtoId MechClothingUiAction= "ActionMechClothingOpenUI"; //SS220-AddMechToClothing
    [DataField]
    public EntProtoId MechClothingGrabAction= "ActionMechClothingGrab"; //SS220-AddMechToClothing
    #endregion

    #region Visualizer States
    [DataField]
    public string? BaseState;
    [DataField]
    public string? OpenState;
    [DataField]
    public string? BrokenState;
    #endregion

    [DataField] public EntityUid? MechCycleActionEntity;
    [DataField] public EntityUid? MechUiActionEntity;
    [DataField] public EntityUid? MechEjectActionEntity;
    [DataField] public EntityUid? MechClothingUiActionEntity; //SS220-AddMechToClothing
    [DataField] public EntityUid? MechClothingGrabActionEntity; //SS220-AddMechToClothing

}
