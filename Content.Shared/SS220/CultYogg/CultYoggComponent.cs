// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;
using Content.Shared.Antag;
using Content.Shared.Roles;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.CultYogg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCultYoggSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class CultYoggComponent : Component, IAntagStatusIconComponent
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

    /// <summary>
    /// Sound played while puking MiGoShroom
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public SoundSpecifier PukeSound = new SoundPathSpecifier("/Audio/SS220/CultYogg/puke.ogg", new()
    {
        MaxDistance = 3
    });

    [ViewVariables, DataField, AutoNetworkedField]
    public string PukedEntity = "FoodMiGomyceteCult"; //what will be puked out

    [ViewVariables, DataField, AutoNetworkedField]
    public string PukedLiquid = "PuddleVomit"; //maybe should be special liquid?

    /// <summary>
    /// The lowest hunger threshold that this mob can be in before it's allowed to digest another shroom.
    /// </summary>
    [DataField("minHungerThreshold")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public HungerThreshold MinHungerThreshold = HungerThreshold.Okay;

    /// <summary>
    /// Entity the cultist will ascend into
    /// </summary>
    public string AscendedEntity = "MiGoCultYogg";

    public int AmountShroomsToAscend = 3; //to check what amount should be for ascencion

    public int ConsumedShrooms = 0; //buffer

    public const int NeededForAscended = 3;//How many shrooms need to be consumed before ascension

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current hunger of the mob doing micoz
    /// </summary>

    [ViewVariables, DataField, AutoNetworkedField]
    public float HungerCost = 5f;

    /// <summary>
    /// The role prototype of the zombie antag role
    /// </summary>
    [DataField("cultYoggRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string CultYoggRoleId = "CultYogg";

    [DataField("cultYoggStatusIcon")]
    public ProtoId<StatusIconPrototype> StatusIcon { get; set; } = "CultYoggFaction";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;
}
public abstract class CultYoggShroomEatenEvent : EntityEventArgs //event for consumed shrooms for cultists
{

}
