// SS220 Changeling
namespace Content.Shared.Changeling;

/// <summary>
/// Raised on a changeling whenever a genome sample is stored. Consumers should use <see cref="NewlyAbsorbed"/>
/// for cumulative objective progress: hive downloads and re-acquiring an old genome do not count twice.
/// </summary>
[ByRefEvent]
public record struct ChangelingGenomeAcquiredEvent(
    string GenomeId,
    EntityUid Source,
    bool NewlyAbsorbed,
    bool CountsForObjective);
