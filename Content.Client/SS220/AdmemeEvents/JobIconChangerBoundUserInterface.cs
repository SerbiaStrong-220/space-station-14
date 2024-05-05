// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.AdmemeEvents.UI;
using Content.Shared.SS220.AdmemeEvents;

namespace Content.Client.SS220.AdmemeEvents;

public sealed class JobIconChangerBoundUserInterface : BoundUserInterface
{
    private JobIconChangerWindow? _window;

    public JobIconChangerBoundUserInterface(EntityUid owner, Enum uiKey)
        : base(owner, uiKey)
    { }

    protected override void Open()
    {
        base.Open();

        _window = new JobIconChangerWindow(this);

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    public void OnJobIconChanged(string newJobIcon)
    {
        SendMessage(new JobIconChangerChangedMessage(newJobIcon));
        _window?.Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not JobIconChangerBoundUserInterfaceState iconChangerState)
            return;

        _window?.OnUpdateState(iconChangerState.Filter);
    }
}
