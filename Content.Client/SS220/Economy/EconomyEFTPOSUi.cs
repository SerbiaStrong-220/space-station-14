// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Economy;

namespace Content.Client.SS220.Economy;

public sealed class EconomyEFTPOSUi(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private readonly EconomyEFTPOSWindow _window = new();

    protected override void Open()
    {
        base.Open();
        _window.OnClose += Close;

        _window.OnPaymentButtonPressed += () =>
        {
            SendMessage(new EconomyEFTPOSPaymentMessage());
        };
        _window.OnPrintReceiptButtonPressed += () =>
        {
            SendMessage(new EconomyEFTPOSPrintReceiptMessage());
        };
        _window.OnKeypadButtonPressed += i =>
        {
            SendMessage(new EconomyEFTPOSKeypadMessage(i));
        };
        _window.OnClearButtonPressed += () =>
        {
            SendMessage(new EconomyEFTPOSKeypadClearMessage());
        };
        _window.OnEnterButtonPressed += () =>
        {
            SendMessage(new EconomyEFTPOSKeypadEnterMessage());
        };
        _window.OnCardLockButtonPressed += i =>
        {
            SendMessage(new EconomyEFTPOSLockMessage(i));
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
