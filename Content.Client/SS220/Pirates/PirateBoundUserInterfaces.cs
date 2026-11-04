using Content.Shared.SS220.Pirates;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Pirates;

public sealed class PirateLootConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PirateLootWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<PirateLootWindow>();
        _window.AppraisePressed += () => SendMessage(new PirateLootAppraiseMessage());
        _window.SellPressed += () => SendMessage(new PirateLootSellMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is PirateLootConsoleState lootState)
            _window?.UpdateState(lootState);
    }
}
