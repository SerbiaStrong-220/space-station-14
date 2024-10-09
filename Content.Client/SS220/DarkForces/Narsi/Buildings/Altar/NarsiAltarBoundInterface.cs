using System;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar;
using Robust.Shared.GameObjects;

namespace Content.Client.SS220.DarkForces.Narsi.Buildings.Altar;

public sealed class NarsiAltarBoundInterface : BoundUserInterface
{
    private NarsiAltarWindow? _window;

    public NarsiAltarBoundInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window?.Dispose();
        _window = new NarsiAltarWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NarsiAltarUIState altarUIState || _window == null)
            return;

        _window.UpdateState(altarUIState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
        }
    }

    public void OpenAbilities()
    {
        SendMessage(new NarsiAltarOpenAbilities());
    }

    public void OpenRituals()
    {
        SendMessage(new NarsiAltarOpenRituals());
    }
}
