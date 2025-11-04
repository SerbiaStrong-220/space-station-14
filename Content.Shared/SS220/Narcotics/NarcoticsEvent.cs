namespace Content.Shared.SS220.Narcotics;

[ByRefEvent]
public record struct MetabolizeNarcoticEvent(EntityUid Body, string NarcoticProto, bool Handled = false);
