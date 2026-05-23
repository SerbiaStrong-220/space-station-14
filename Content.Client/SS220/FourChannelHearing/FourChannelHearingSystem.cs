using Content.Shared.SS220.FourChannelHearing;
using Robust.Client.Graphics;

namespace Content.Client.SS220.FourChannelHearing;

public sealed class FourChannelHearingSystem : SharedFourChannelHearingSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private FourChannelHearingOverlayAlt? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FourChannelHearingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FourChannelHearingComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<FourChannelHearingComponent> entity, ref ComponentInit args)
    {
        if (_overlay != null)
            return;

        _overlay = new();
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnRemove(Entity<FourChannelHearingComponent> entity, ref ComponentRemove args)
    {
        if (_overlay == null)
            return;

        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Dispose();
        _overlay = null;
    }
}
