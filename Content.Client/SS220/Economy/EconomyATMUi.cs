// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Economy;

namespace Content.Client.SS220.Economy;

public sealed class EconomyATMUi(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private readonly EconomyATMWindow _window = new();

    protected override void Open()
    {
        base.Open();
        _window.OnClose += Close;

        _window.OnKeypadButtonPressed += i =>
        {
            SendMessage(new EconomyATMKeypadMessage(i));
        };
        _window.OnClearButtonPressed += () =>
        {
            SendMessage(new EconomyATMKeypadClearMessage());
        };
        _window.OnEnterButtonPressed += i =>
        {
            SendMessage(new EconomyATMKeypadEnterMessage(i));
        };
        _window.OnLinkButtonPressed += () =>
        {
            SendMessage(new EconomyATMBankAccountLinkMessage());
        };
        _window.OnCreateButtonPressed += () =>
        {
            SendMessage(new EconomyATMBankAccountCreateMessage());
        };

        if (State != null)
        {
            UpdateState(State);
        }

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window.UpdateState(state);
    }

    protected override void Dispose(bool disposing)
    {
        _window.Close();
        base.Dispose(disposing);
    }
}
