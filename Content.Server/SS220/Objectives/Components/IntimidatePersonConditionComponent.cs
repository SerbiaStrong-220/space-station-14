// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Trackers.Components;
using Content.Shared.Mind.Filters;

namespace Content.Server.SS220.Objectives.Components;

[RegisterComponent]
public sealed partial class IntimidatePersonConditionComponent : Component
{
    [DataField(required: true)]
    public DamageTrackerSpecifier DamageTrackerSpecifier = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid TargetMob;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ObjectiveIsDone = false;

    public IntimidatePersonDescriptionType DescriptionType;
    // Descriptions comes to help player differ done object from one which isn't.

    /// <summary>
    /// Description will be applied at start.
    /// </summary>
    [DataField(required: true)]
    public string? StartDescription;

    /// <summary>
    /// Description will be applied when objective is done.
    /// </summary>
    [DataField(required: true)]
    public string? SuccessDescription;

    /// <summary>
    /// Description will be applied if target gets SSDs.
    /// </summary>
    [DataField(required: true)]
    public string? SSDDescription;

    [DataField]
    public MindPool Pool = new AliveHumansPool();

    /// <summary>
    /// Filters to apply to <see cref="Pool"/>.
    /// </summary>
    [DataField]
    public List<MindFilter> Filters = new();
}

public enum IntimidatePersonDescriptionType
{
    Start = 0,
    Success,
    SSD
}
