using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.SS220.Cartridges.Timer;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Cartridges;

public sealed partial class TimerCartridgeUi : UIFragment
{
    private TimerCartridgeUiFragment? _fragment;
    private BoundUserInterface _userInterface = default!;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _userInterface = userInterface;

        _fragment = new();
        _fragment.OnTimerEnable += OnTimerEnable;
        _fragment.OnTimerDisable += OnTimerDisable;
        _fragment.OnNotifyToggle += OnNotifyToggle;
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not TimerCartridgeUiState timerState)
            return;

        _fragment?.UpdateState(timerState);
    }

    private void OnTimerEnable(TimeSpan timer)
    {
        _userInterface.SendMessage(new CartridgeUiMessage(new TimerCartridgeUiMessageEvent()
        {
            Action = TimerCartridgeUiAction.EnableTimer,
            Timer = timer,
            TimerNotify = _fragment!.TimerNotify,
        }));
    }

    private void OnTimerDisable()
    {
        _userInterface.SendMessage(new CartridgeUiMessage(new TimerCartridgeUiMessageEvent()
        {
            Action = TimerCartridgeUiAction.DisableTimer,
            Timer = _fragment!.Timer,
            TimerNotify = _fragment!.TimerNotify,
        }));
    }

    private void OnNotifyToggle()
    {
        _userInterface.SendMessage(new CartridgeUiMessage(new TimerCartridgeUiMessageEvent()
        {
            Action = TimerCartridgeUiAction.ToggleNotification,
            Timer = _fragment!.Timer,
            TimerNotify = _fragment!.TimerNotify,
        }));
    }
}
