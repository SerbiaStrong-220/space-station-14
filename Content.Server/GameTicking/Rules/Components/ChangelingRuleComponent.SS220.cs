// SS220 Changeling
using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

public sealed partial class ChangelingRuleComponent
{
    [DataField]
    public EntProtoId<ObjectiveComponent> AbsorbDnaObjective = "ChangelingAbsorbDnaObjective";

    [DataField]
    public EntProtoId<ObjectiveComponent> StealBrainObjective = "ChangelingStealBrainObjective";

    [DataField]
    public float StealBrainChance = 0.5f;

    [DataField]
    public List<EntProtoId<ObjectiveComponent>> ValuableItemObjectives =
    [
        "ChangelingCMOHyposprayStealObjective",
        "ChangelingCMOCrewMonitorStealObjective",
        "ChangelingRDHardsuitStealObjective",
        "ChangelingHandTeleporterStealObjective",
        "ChangelingMagbootsStealObjective",
        "ChangelingCaptainIDStealObjective",
        "ChangelingCaptainJetpackStealObjective",
        "ChangelingCaptainGunStealObjective",
        "ChangelingNukeDiskStealObjective",
    ];

    [DataField]
    public EntProtoId<ObjectiveComponent> KillAiObjective = "ChangelingKillStationAiObjective";

    [DataField]
    public EntProtoId<ObjectiveComponent> KillAndImpersonateObjective = "ChangelingKillAndImpersonateObjective";

    [DataField]
    public EntProtoId<ObjectiveComponent> EscapeObjective = "ChangelingEscapeShuttleObjective";
}
