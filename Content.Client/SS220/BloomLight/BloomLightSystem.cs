// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CCVars;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.BloomLight;

public sealed partial class BloomLightSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private BloomLightOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new(EntityManager, _prototype);
        _cfg.OnValueChanged(CCVars220.BloomLightingEnabled, SetOverlayEnabled, true);
    }

    public void SetOverlayEnabled(bool enabled)
    {
        var hasOverlay = _overlayManager.HasOverlay<BloomLightOverlay>();

        if (enabled)
        {
            if (!hasOverlay)
                _overlayManager.AddOverlay(_overlay);
        }
        else
        {
            if (hasOverlay)
                _overlayManager.RemoveOverlay(_overlay);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        SetOverlayEnabled(false);
        _cfg.UnsubValueChanged(CCVars220.BloomLightingEnabled, SetOverlayEnabled);
    }
}
