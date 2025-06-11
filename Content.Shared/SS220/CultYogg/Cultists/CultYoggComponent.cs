// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid.Markings;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.Cultists;

/// <summary>
/// Component of the Cult Yogg cultist.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCultYoggSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class CultYoggComponent : Component
{
    #region abilities
    [DataField]
    public EntProtoId PukeShroomAction = "ActionCultYoggPukeShroom";

    [DataField]
    public EntProtoId DigestAction = "ActionCultYoggDigest";

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
    #endregion

    #region puke
    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current hunger of the mob doing micoz
    /// </summary>

    [ViewVariables, DataField, AutoNetworkedField]
    public float HungerCost = 25f;

    [ViewVariables, DataField, AutoNetworkedField]
    public float ThirstCost = 50f;

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
    #endregion

    #region ascension
    /// <summary>
    /// Entity the cultist will ascend into
    /// </summary>
    [ViewVariables]
    public string AscendedEntity = "MiGo";

    [ViewVariables]
    public float AmountAscensionReagentAscend = 6f; // This is equal to 3 shrooms
    [ViewVariables, Access(Other = AccessPermissions.ReadWrite)]
    public float ConsumedAscensionReagent = 0; //buffer
    #endregion

    #region stages
    [DataField]
    public Color? PreviousEyeColor;

    [DataField]
    public Marking? PreviousTail;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public CultYoggStage CurrentStage = CultYoggStage.Initial;
    #endregion

    /// <summary>
    /// Visual effect to spawn when entity corrupted
    /// </summary>
    [DataField]
    public EntProtoId CorruptionEffect = "CorruptingEffect";
}
