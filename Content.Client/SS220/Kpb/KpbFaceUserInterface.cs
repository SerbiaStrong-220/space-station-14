using System;
using Content.Shared.SS220.Kpb;
using Robust.Client.GameObjects;
using Content.Shared.UserInterface;

namespace Content.Client.SS220.Kpb;

public sealed class KpbFaceUserInterface : BoundUserInterface
{
    private KpbFaceMenu? _menu;

    public KpbFaceUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = new KpbFaceMenu();
        _menu.OnClose += Close;
        _menu.FaceSelected += state =>
        {
            SendPredictedMessage(new KpbFaceSelectMessage(state));
            Close();
        };
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState? state)
    {
        if (state != null)
            base.UpdateState(state);
        if (_menu == null || state is not KpbFaceBuiState msg)
            return;
        _menu.Populate(msg.Profile, msg.Selected);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
        _menu = null;
    }
}
