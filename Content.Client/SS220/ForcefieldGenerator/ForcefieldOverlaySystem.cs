// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.ForcefieldGenerator;

public sealed class ForcefieldOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private ForcefieldOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new(this.EntityManager, _prototype);
        _overlayManager.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
    }
}
