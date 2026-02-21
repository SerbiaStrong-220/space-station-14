using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Cartridges.Timer;

[Serializable, NetSerializable]
public sealed class TimerCartridgeUiState : BoundUserInterfaceState
{
    public DateTime DateTime;
    public TimeSpan ShiftLength;

    /// Whether or not PDA should play notification when timer ends
    public bool TimerNotify;

    public TimeSpan Timer = TimeSpan.Zero;
    public bool TimerActive;

    public TimerCartridgeUiState(DateTime date, TimeSpan shiftLength, bool timerNotify, TimeSpan timer, bool timerActive)
    {
        DateTime = date;
        ShiftLength = shiftLength;
        TimerNotify = timerNotify;
        Timer = timer;
        TimerActive = timerActive;
    }
}
