// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[ByRefEvent]
public record struct PathologyAddedAttempt(ProtoId<PathologyPrototype> PathologyId, bool Cancelled = false);

[ByRefEvent]
public readonly record struct PathologyAddedEvent(ProtoId<PathologyPrototype> PathologyId);

[ByRefEvent]
public readonly record struct PathologySeverityChanged(ProtoId<PathologyPrototype> PathologyId, int PreviousSeverity, int CurrentSeverity);

/// <summary>
/// Raised on pathology owner, count for pathology instance not changed in time of raising event
/// </summary>
[ByRefEvent]
public readonly record struct PathologyStackCountChanged(ProtoId<PathologyPrototype> PathologyId, int Severity, int PreviousCount, int NewCount);

[ByRefEvent]
public record struct PathologyRemoveAttempt(ProtoId<PathologyPrototype> PathologyId, int CurrentSeverity, bool Cancelled = false);

[ByRefEvent]
public readonly record struct PathologyRemovedEvent(ProtoId<PathologyPrototype> PathologyId);

// related to pathology, but not the main logic

[ByRefEvent]
public record struct GetPathologyHealerDamageModifier(ProtoId<PathologyPrototype> PathologyId, EntityUid Owner, EntityUid Target, float Modifier = 1f);
