// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.SMES.UI;
using Content.Shared.SMES;

namespace Content.Client.SS220.SMES;

public sealed class SmesBoundUserInterface : BoundUserInterface
{
    private SmesWindow? _window;

    public SmesBoundUserInterface(EntityUid owner, Enum uiKey)
        : base(owner, uiKey)
    { }

    protected override void Open()
    {
        base.Open();

        _window = new SmesWindow();

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not SmesState castedState)
            return;

        _window?.UpdateState(castedState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}
