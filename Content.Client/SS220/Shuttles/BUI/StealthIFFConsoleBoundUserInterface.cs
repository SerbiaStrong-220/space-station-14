using Content.Client.SS220.Shuttles.UI;
using Content.Shared.SS220.Shuttles.BUIStates;
using Content.Shared.SS220.Shuttles.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Shuttles.BUI;

[UsedImplicitly]
public sealed class StealthIFFConsoleBoundUserInterface : BoundUserInterface
{
    private StealthIFFConsoleWindow? _window;

    public StealthIFFConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<StealthIFFConsoleWindow>();
        _window.ActivateStealth += SendActivateMessage;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is StealthIFFConsoleBoundUserInterfaceState bState)
            _window?.UpdateState(bState);
    }

    private void SendActivateMessage()
    {
        SendMessage(new ActivateStealthIFFMessage());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Close();
        _window = null;
    }
}
