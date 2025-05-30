// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.NPC.Prototypes;
using Content.Shared.SS220.CultYogg.Altar;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.SS220.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(CultYoggRuleSystem))]
public sealed partial class CultYoggRuleComponent : Component
{
    [DataField]
    public Dictionary<CultYoggStage, CultYoggStageDefinition> Stages { get; private set; } = new();

    /// <summary>
    /// General requirements
    /// </summary>

    public readonly List<string> FirstTierJobs = ["Captain"];
    public readonly string SecondTierDepartament = "Command";
    public readonly List<string> BannedDepartaents = ["GhostRoles"];

    public bool SacraficialsWerePicked = false;//buffer to prevent multiple generations

    /// <summary>
    /// Where previous sacrificial were performed
    /// </summary>
    public Entity<CultYoggAltarComponent>? LastSacrificialAltar = null;

    public int InitialCrewCount;
    public int TotalCultistsConverted;

    /// <summary>
    /// Storages for an endgame screen title
    /// </summary>
    public readonly List<EntityUid> InitialCultistMinds = []; //Who was cultist on the gamestart.

    /// <summary>
    /// Storage for a sacraficials
    /// </summary>
    public readonly int[] TierOfSacraficials = [1, 2, 3];//trying to save tier in target, so they might be replaced with the same lvl target

    /// <summary>
    /// Groups and factions
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> CultYoggFaction = "CultYogg";

    /// <summary>
    /// Variables required to make new cultists
    /// </summary>
    [DataField]
    public List<string> ListofObjectives = ["CultYoggSacraficeObjective"];

    [ValidatePrototypeId<EntityPrototype>]
    public string MindCultYoggAntagId = "MindRoleCultYogg";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GodPrototype = "MobNyarlathotep";

    //telephaty channel
    [DataField]
    public string TelepathyChannel = "TelepathyChannelYoggSothothCult";

    [DataField]
    public EntityWhitelist WhitelistToggleable = new EntityWhitelist
    {
        Tags = new() { "CultYoggInnerHandToggleable" }
    };
    /// <summary>
    /// Check for an endgame screen title
    /// </summary>
    [DataField]
    public int AmountOfSacrifices = 0;

    [DataField]
    public bool Summoned = false;

    [DataField]
    public SoundSpecifier SummonMusic = new SoundCollectionSpecifier("CultYoggMusic");//ToDo make own
    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToStart = 1,
        Started = 2,
    }

    /// <summary>
    /// Current state of the rule
    /// </summary>
    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;

    /// <summary>
    /// Current cult gameplay stage
    /// </summary>
    public CultYoggStage Stage = CultYoggStage.Initial;

    /// <summary>
    /// When should cultists be selected and the announcement made
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? AnnounceAt;

    /// <summary>
    ///     Path to cultist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/SS220/Ambience/Antag/cult_yogg_start.ogg");
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
    /// Fraction of total crew converted to cultists that will progress cult to this stage.
    /// </summary>
    [DataField]
    public FixedPoint2? CultistsFractionRequired;
}

