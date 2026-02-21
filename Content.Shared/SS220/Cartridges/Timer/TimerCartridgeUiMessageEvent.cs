using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Cartridges.Timer;

[Serializable, NetSerializable]
public sealed partial class TimerCartridgeUiMessageEvent : CartridgeMessageEvent
{
    [DataField]
    public TimerCartridgeUiAction Action;

    [DataField]
    public TimeSpan Timer;

    [DataField]
    public bool TimerNotify;
}

public enum TimerCartridgeUiAction
{
    EnableTimer,
    DisableTimer,
    ToggleNotification
}
