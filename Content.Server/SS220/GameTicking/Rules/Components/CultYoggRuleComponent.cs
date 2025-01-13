// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.SS220.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(CultYoggRuleSystem))]
public sealed partial class CultYoggRuleComponent : Component
{
    /// <summary>
    /// General requirements
    /// </summary>
    [DataField]
    public int AmountOfSacrificesToGodSummon = 3;

    [DataField]
    public int AmountOfSacrificesToWarningAnouncement = 2;

    [DataField]
    public int ReqAmountOfMiGo = 3;

    /// <summary>
    /// General requirements
    /// </summary>

    public readonly List<string> FirstTierJobs = ["Captain"];
    public readonly string SecondTierDepartament = "Command";
    public readonly List<string> BannedDepartaents = ["GhostRoles"];

    public bool SacraficialsWerePicked = false;//buffer to prevent multiple generations

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
    /// <summary>
    /// Check for an endgame screen title
    /// </summary>
    [DataField]
    public int AmountOfSacrifices = 0;

    [DataField]
    public bool Summoned = false;

    [DataField("summonMusic")]
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
