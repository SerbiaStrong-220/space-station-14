// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

namespace Content.Shared.SS220.Movement.Events;

[ByRefEvent]
public record struct CanApplyEyeCursorOffsetEvent( bool Cancelled);
