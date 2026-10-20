// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.TapeRecorder.Components;

namespace Content.Shared.SS220.TapeRecorder;

/// <summary>
/// Raised before a recorder starts an active mode, allowing power requirements to reject it.
/// </summary>
[ByRefEvent]
public record struct TapeRecorderModeChangeAttemptEvent(TapeRecorderMode Mode, bool Cancelled = false);

/// <summary>
/// Raised after the recorder mode changes so power consumption can be updated.
/// </summary>
[ByRefEvent]
public readonly record struct TapeRecorderModeChangedEvent(TapeRecorderMode Mode);
