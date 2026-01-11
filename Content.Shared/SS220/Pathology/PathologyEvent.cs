// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[ByRefEvent]
public record struct PathologyAddedAttempt(ProtoId<PathologyPrototype> PathologyId, bool Cancelled = false);

[ByRefEvent]
public record struct PathologyAddedEvent(ProtoId<PathologyPrototype> PathologyId);

[ByRefEvent]
public record struct PathologySeverityChanged(ProtoId<PathologyPrototype> PathologyId, int PreviousSeverity, int CurrentSeverity);

[ByRefEvent]
public record struct PathologyRemoveAttempt(ProtoId<PathologyPrototype> PathologyId, int CurrentSeverity, bool Cancelled = false);

[ByRefEvent]
public record struct PathologyRemoveEvent(ProtoId<PathologyPrototype> PathologyId);

