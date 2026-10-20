using System;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;
using Robust.Shared.GameObjects;

namespace Content.Client.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

public sealed class NarsiRitualsBoundInterface : BoundUserInterface
{
    private NarsiRitualsWindow? _window;

    public NarsiRitualsBoundInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window?.Dispose();
        _window = new NarsiRitualsWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NarsiRitualsState ritualsState || _window == null)
            return;

        _window.UpdateState(ritualsState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }

    public void OnStartRitualPressed(string ritualPrototype)
    {
        SendMessage(new NarsiAltarStartRitualEvent(ritualPrototype));
    }
}
