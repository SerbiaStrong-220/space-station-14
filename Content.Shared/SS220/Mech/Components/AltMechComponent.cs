// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.Mech.Components;

/// <summary>
/// A large, pilotable machine that has equipment that is
/// powered via an internal battery.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
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
    /// Maximal mass of an arm that can be installed into this mech frame
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaximalArmMass = 0;

    /// <summary>
    /// The maximum amount of energy the mech can have.
    /// Derived from the currently inserted battery.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxEnergy = 0;

    /// <summary>
    /// A container for storing the equipment entities.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Container EquipmentContainer = new();

    [ViewVariables]
    public readonly string EquipmentContainerId = "part-mech-equipment-container";

    [DataField, AutoNetworkedField]
    public int CurrentEquipmentAmount = 0;

    /// <summary>
    /// The maximum amount of equipment items that can be installed in the mech
    /// </summary>
    [DataField("maxEquipmentAmount"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxEquipmentAmount = 100;

    /// <summary>
    /// A whitelist for inserting equipment items.
    /// </summary>
    [DataField]
    public EntityWhitelist? EquipmentWhitelist;

    /// <summary>
    /// Whether the mech has been destroyed and is no longer pilotable.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Broken = false;

    /// <summary>
    /// If the mech is turned on
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Online = false;

    /// <summary>
    /// The slot the pilot is stored in.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot PilotSlot = default!;

    [ViewVariables]
    public readonly string PilotSlotId = "mech-pilot-slot";

    /// <summary>
    /// The slot the pilot is stored in.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot TankSlot = default!;

    [ViewVariables]
    public readonly string TankSlotId = "mech-tank-slot";// for tanks with breathing mix

    [DataField, AutoNetworkedField]
    public float OverallBaseMovementSpeed = 0f;

    [DataField, AutoNetworkedField]
    public float MovementSpeedModifier = 0f;

    [DataField, AutoNetworkedField]
    public float OverallBaseAcceleration = 1f;

    [AutoNetworkedField]
    public bool MaintenanceMode = true; //if the mech is not in the maintenance mode we cannot interact with its parts or equipment

    [AutoNetworkedField]
    public bool Bolted = false; //if the mech is bolted you won't be able to pull the pilot out

    [AutoNetworkedField]
    public bool BoltsSawed = false; //if the mech's bolts are sawed off bolts don't work

    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, ContainerSlot> ContainerDict = new Dictionary<string, ContainerSlot>();

    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> ContainersToCreate = new List<string>{ "head", "right-arm", "left-arm", "chassis", "power" };

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> SlotsToDrop = new List<string> { "shoes", "outerClothing", "gloves", "neck", "mask", "eyes", "head", "pocket1", "pocket2", "suitstorage", "belt", "back" };// items from those slots will be dropped on mech enter. Intended to be everything except for PDA, inner clothing and headset

    //List of the user's hands that must be given back when leaving the mech 
    [DataField]
    public Dictionary<string, Hand> Hands = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan NextPowerDrain = TimeSpan.Zero;

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
    [AutoNetworkedField]
    public bool Airtight;

    /// <summary>
    /// Can this mech be sealed
    /// </summary>
    [DataField]
    public bool Sealable = true;

    /// <summary>
    /// Can the user see without mech optics
    /// </summary>
    [DataField]
    public bool Transparent = false;

    /// <summary>
    /// The sound to be played when mech is bolted
    /// </summary>
    [DataField]
    public SoundSpecifier BoltSound =
        new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg")
        {
            Params = AudioParams.Default
        };

    /// <summary>
    /// The sound to be played when is unbolted
    /// </summary>
    [DataField]
    public SoundSpecifier UnboltSound =
        new SoundPathSpecifier("/Audio/Machines/boltsup.ogg")
        {
            Params = AudioParams.Default
        };

    /// <summary>
    /// The sound to be played when is unbolted
    /// </summary>
    [DataField]
    public SoundSpecifier SealSound =
        new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg")
        {
            Params = AudioParams.Default
        };

    #region Action Prototypes
    [DataField]
    public EntProtoId MechCycleAction = "ActionMechCycleEquipment";
    [DataField]
    public EntProtoId MechUiAction = "ActionMechOpenUI";
    [DataField]
    public EntProtoId MechEjectAction = "ActionMechEject";

    //[DataField]
    //public EntProtoId PilotUiAction = "ActionPilotOpenUI";//Why? Because mech and pilot couldn't have same actions so or this or adding/deleting actions aaaall the way
    //[DataField]
    //public EntProtoId PilotEjectAction = "ActionPilotEject";
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
    //[DataField] public EntityUid? PilotUiActionEntity;
    //[DataField] public EntityUid? PilotEjectActionEntity;

}
