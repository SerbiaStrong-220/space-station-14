// SS220 Changeling
namespace Content.Server.Changeling.Objectives;

/// <summary>
/// Requires the changeling to acquire a cumulative number of unique genomes.
/// Hive downloads and genomes that were already counted must not be included in this total.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingAbsorbDnaConditionComponent : Component
{
    [DataField]
    public int MinGenomes = 6;

    [DataField]
    public int MaxGenomes = 8;

    [DataField]
    public int TargetGenomes;
}

/// <summary>
/// Requires possession of the exact brain selected when the objective is assigned.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingStealBrainConditionComponent : Component
{
    [DataField]
    public EntityUid? TargetMind;

    [DataField]
    public EntityUid? TargetBody;

    [DataField]
    public EntityUid? TargetBrain;
}

/// <summary>
/// Requires a target to be dead and the changeling to escape while either wearing the target's genome or
/// carrying the target's original ID card.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingKillAndImpersonateConditionComponent : Component
{
    [DataField]
    public EntityUid? TargetMind;

    [DataField]
    public EntityUid? TargetBody;

    [DataField]
    public EntityUid? TargetIdCard;

    [DataField]
    public string? TargetGenome;
}

/// <summary>
/// Selects and tracks a living station AI belonging to the changeling's station.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingKillStationAiConditionComponent : Component
{
    [DataField]
    public EntityUid? TargetMind;
}

/// <summary>
/// Requires the changeling to be alive aboard the emergency shuttle. Restraints do not affect completion.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingEscapeAliveConditionComponent : Component;
