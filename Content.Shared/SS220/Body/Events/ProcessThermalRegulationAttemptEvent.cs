namespace Content.Shared.SS220.Body.Events;

[ByRefEvent]
public record struct ProcessThermalRegulationAttemptEvent(bool Cancelled = false);
