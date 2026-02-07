// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.SS220.CultYogg.Altar;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(CultYoggRuleSystem))]
public sealed partial class CultYoggRuleComponent : Component
{
    #region GameRuleSelection
    /// <summary>
    /// Current state of the rule
    /// </summary>
    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;

    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToStart = 1,
        Started = 2,
    }
    #endregion

    #region Stage
    [DataField]
    public Dictionary<CultYoggStage, CultYoggStageDefinition> Stages { get; private set; } = [];

    /// <summary>
    /// Current cult gameplay stage
    /// </summary>
    public CultYoggStage Stage = CultYoggStage.Initial;

    /// <summary>
    /// Time for the cultists before the whole station finds out about them
    /// </summary>
    [DataField]
    public TimeSpan BeforeAlertTime = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The time when the announcement will be made
    /// </summary>
    public TimeSpan? AlertTime;

    /// <summary>
    /// Station notification sound when Alert stage start message is announced
    /// </summary>
    public SoundSpecifier BroadcastSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");
    #endregion

    #region Sacrificial
    /// <summary>
    /// General requirements
    /// </summary>
    public readonly ProtoId<DepartmentPrototype> SacrificialDepartament = "Command";

    /// <summary>
    /// Shows how many sacrifices were made
    /// </summary>
    [DataField]
    public int AmountOfSacrifices = 0;
    #endregion

    /// <summary>
    /// Internal variable for storing the estimated number of station crew
    /// </summary>
    public int InitialCrewCount;
    public int TotalCultistsConverted;

    /// <summary>
    /// Storages for an endgame screen title
    /// </summary>
    public readonly List<EntityUid> InitialCultistMinds = []; //Who was cultist on the gamestart.

    #region CultistMaking
    /// <summary>
    ///     Path to cultist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/SS220/Ambience/Antag/cult_yogg_start.ogg");

    /// <summary>
    /// Groups and factions
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> CultYoggFaction = "CultYogg";

    public EntProtoId MindCultYoggAntagId = "MindRoleCultYogg";

    //telephaty channel
    [DataField]
    public string TelepathyChannel = "TelepathyChannelYoggSothothCult";

    [DataField]
    public EntityWhitelist WhitelistToggleable = new()
    {
        Tags = ["CultYoggInnerHandToggleable"]
    };
    #endregion

    #region GodSummoning
    /// <summary>
    /// That which will be called upon at the final sacrifice
    /// </summary>
    [DataField]
    public EntProtoId GodPrototype = "MobNyarlathotep";

    /// <summary>
    /// Where previous sacrificial were performed
    /// </summary>
    public Entity<CultYoggAltarComponent>? LastSacrificialAltar = null;

    /// <summary>
    /// The music that will play when you summon a god
    /// </summary>
    [DataField]
    public SoundSpecifier SummonMusic = new SoundCollectionSpecifier("CultYoggMusic");
    #endregion
}

[DataDefinition]
public sealed partial class CultYoggStageDefinition
{
    /// <summary>
    /// Amount of sacrifices that will progress cult to this stage.
    /// </summary>
    [DataField]
    public int? SacrificesRequired;

    /// <summary>
    /// The percentage of the entire crew converted to the cult that will advance the cult to this stage.
    /// </summary>
    [DataField]
    public FixedPoint2? CultistsToCrewFraction;

    /// <summary>
    /// Direct calculation of required cultist stages for progression to avoid round-start progression.
    /// </summary>
    public int? CultistsAmountRequired;

    /// <summary>
    /// Objectives given upon reaching the stage.
    /// </summary>
    [DataField]
    public List<EntProtoId>? StageObjectives;
}

