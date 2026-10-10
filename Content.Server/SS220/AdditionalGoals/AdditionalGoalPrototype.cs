// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.AdditionalGoals;

/// <summary>
/// An optional department assignment, printed at round start for its head.
/// Completion is left to roleplay; these are not antagonist objectives.
/// </summary>
[Prototype]
public sealed partial class AdditionalGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;

    /// <summary>
    /// Localization key for the assignment. Supports the station and head arguments.
    /// </summary>
    [DataField(required: true)]
    public LocId Text;

    /// <summary>
    /// Chance for this goal to be selected. A failed roll means that no goal is
    /// sent for this department on this station.
    /// </summary>
    [DataField]
    public float Chance = 0.5f;

    /// <summary>
    /// Optional runtime value that is substituted into the goal text.
    /// </summary>
    [DataField]
    public AdditionalGoalTarget Target = AdditionalGoalTarget.None;
}

public enum AdditionalGoalTarget : byte
{
    None,
    RandomStationEmployee,
}
