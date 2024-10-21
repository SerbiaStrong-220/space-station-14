// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.SS220.CultYogg.EntitySystems;

namespace Content.Shared.SS220.CultYogg.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCultYoggSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class CultYoggComponent : Component
{
    /// ABILITIES ///
    [DataField]
    public EntProtoId PukeShroomAction = "ActionCultYoggPukeShroom";

    [DataField]
    public EntProtoId DigestAction = "ActionCultYoggDigest";

    [DataField]
    public EntProtoId AscendingAction = "ActionCultYoggAscending";

    [DataField]
    public EntProtoId CorruptItemAction = "ActionCultYoggCorruptItem";

    [DataField]
    public EntProtoId CorruptItemInHandAction = "ActionCultYoggCorruptItemInHand";

    [DataField, AutoNetworkedField]
    public EntityUid? PukeShroomActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? DigestActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? CorruptItemActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? CorruptItemInHandActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? AscendingActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public int CurrentStage = 0;

    [ViewVariables, DataField, AutoNetworkedField]
    public string PukedEntity = "FoodMiGomyceteCult"; //what will be puked out

    /// <summary>
    /// The lowest hunger threshold that this mob can be in before it's allowed to digest another shroom.
    /// </summary>
    [DataField("minHungerThreshold"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public HungerThreshold MinHungerThreshold = HungerThreshold.Starving;

    /// <summary>
    /// The lowest thirst threshold that this mob can be in before it's allowed to digest another shroom.
    /// </summary>
    [DataField("minThirstThreshold"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public ThirstThreshold MinThirstThreshold = ThirstThreshold.Parched;

    /// <summary>
    /// Entity the cultist will ascend into
    /// </summary>
    public string AscendedEntity = "MiGoCultYogg";

    public int AmountShroomsToAscend = 3; //to check what amount should be for ascencion

    public int ConsumedShrooms = 0; //buffer

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current hunger of the mob doing micoz
    /// </summary>

    [ViewVariables, DataField, AutoNetworkedField]
    public float HungerCost = 50f;

    [ViewVariables, DataField, AutoNetworkedField]
    public float ThirstCost = 100f;

    /// <summary>
    /// The role prototype of the culsist antag role
    /// </summary>
    [DataField("cultYoggRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string CultYoggRoleId = "CultYogg";

    [DataField]
    public Color? PreviousEyeColor;


    [DataField]
    public Marking? PreviousTail;
}
