using Content.Shared.SS220.QuadHearing;
using Robust.Client.Graphics;

namespace Content.Client.SS220.QuadHearing;

public sealed class QuadHearingSystem : SharedQuadHearingSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private QuadHearingOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<QuadHearingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<QuadHearingComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(Entity<QuadHearingComponent> entity, ref ComponentInit args)
    {
        if (_overlay != null)
            return;

        _overlay = new();
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnRemove(Entity<QuadHearingComponent> entity, ref ComponentRemove args)
    {
        if (_overlay == null)
            return;

        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Dispose();
        _overlay = null;
    }
}
