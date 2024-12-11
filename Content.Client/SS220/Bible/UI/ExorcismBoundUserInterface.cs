// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Bible;

namespace Content.Client.SS220.Bible.UI;

public sealed class ExorcismBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ExorcismMenu? _menu;

    [ViewVariables]
    public int LengthMin { get; private set; }
    [ViewVariables]
    public int LengthMax { get; private set; }


    public ExorcismBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        _menu = new ExorcismMenu(this);
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    public void ReadButtonPressed(string message)
    {
        SendMessage(new ExorcismReadMessage(message));
        _menu?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not ExorcismInterfaceState exorcismState)
            return;

        LengthMin = exorcismState.LengthMin;
        LengthMax = exorcismState.LengthMax;
        _menu?.RefreshLengthCounter();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _menu?.Dispose();
    }
}
