namespace Content.Server.SS220.DarkForces.Narsi.Progress;

[RegisterComponent]
public sealed partial class NarsiCultProgressComponent : Component
{
    [DataField]
    public Dictionary<string, int> OpenedAbilities = new();

    [DataField]
    public int BloodPoints;

    [DataField]
    public EntityUid? LeaderEntity;

    [DataField]
    public LeaderState LeaderState = LeaderState.Idle;

    [DataField(required: true)]
    public NarsiObjectivesData NarsiObjectives = new();
}

[DataDefinition]
public sealed partial class NarsiObjectivesData
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanSummonNarsi;

    [DataField]
    public List<EntityUid> Objectives = new();

    [DataField]
    public EntityUid? NarsiSummonObjective;

    [DataField]
    public TimeSpan NarsiObjectivesCheckTime;

    [DataField]
    public TimeSpan NarsiObjectivesCheckThreshold = TimeSpan.FromSeconds(30);

    [DataField]
    public int MaxKills;

    [DataField]
    public int MinKills;

    [DataField]
    public int MaxRituals;

    [DataField]
    public int MinRituals;
}

public enum LeaderState
{
    Idle,
    Dead,
    Selected,
    NoCandidates,
}
