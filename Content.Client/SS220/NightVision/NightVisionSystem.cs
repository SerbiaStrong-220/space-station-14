using Content.Client.Light;
using Content.Shared.SS220.NightVision;
using Robust.Client.Graphics;

namespace Content.Client.SS220.NightVision;

public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    protected override void EnableOverlay(Entity<NightVisionComponent> ent)
    {
        if (!GameTiming.IsFirstTimePredicted)
            return;

        var afterLightOverlay = _overlay.GetOverlay<AfterLightTargetOverlay>();
        afterLightOverlay.NightVisionEnabled = true;
        afterLightOverlay.MinLightAfterTargetOverlay = ent.Comp.MinLightAfterTargetOverlay;

        _overlay.AddOverlay(new NightVisionColorOverlay
        {
            MinLight = ent.Comp.MinLight,
            BrightThreshold = ent.Comp.BrightThreshold,
            BrightBoost = ent.Comp.BrightBoost,
            Gamma = ent.Comp.Gamma,
            NoiseAmount = ent.Comp.NoiseAmount,
        });
    }

    protected override void DisableOverlay()
    {
        if (!GameTiming.IsFirstTimePredicted)
            return;

        _overlay.GetOverlay<AfterLightTargetOverlay>().NightVisionEnabled = false;
        _overlay.RemoveOverlay<NightVisionColorOverlay>();
    }
}
