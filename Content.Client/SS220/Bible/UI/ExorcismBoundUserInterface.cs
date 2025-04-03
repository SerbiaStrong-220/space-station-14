// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Bible;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Bible.UI;

public sealed class ExorcismBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ExorcismMenu? _menu;

    public ExorcismBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ExorcismMenu>();
        _menu.ReadClicked += OnReadButtonPressed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not ExorcismInterfaceState exorcismState)
            return;
        if (_menu == null)
            return;

        _menu.LengthMin = exorcismState.LengthMin;
        _menu.LengthMax = exorcismState.LengthMax;
        _menu.RefreshLengthCounter();
    }

    private void OnReadButtonPressed(string message)
    {
        SendMessage(new ExorcismReadMessage(message));
        Close();
    }
}
