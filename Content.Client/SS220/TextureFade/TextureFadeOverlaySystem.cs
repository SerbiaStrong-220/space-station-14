// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.SS220.TextureFade;

public sealed class TextureFadeOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TextureFadeOverlayComponent, ComponentRemove>(OnRemove);
    }

    public override void FrameUpdate(float frameTime)
    {
        var componentsQuery = EntityQueryEnumerator<TextureFadeOverlayComponent>();
        while (componentsQuery.MoveNext(out var comp))
        {
            HandleOverlayActivityUpdate(comp);
            HandleOverlayProgressUpdate(comp, frameTime);
        }
    }

    private void OnRemove(Entity<TextureFadeOverlayComponent> entity, ref ComponentRemove args)
    {
        DestroyOverlay(entity.Comp);
    }

    private void HandleOverlayActivityUpdate(TextureFadeOverlayComponent component)
    {
        if (component.IsEnabled && component.Overlay is null)
        {
            component.Overlay = CreateOverlay(component);
            return;
        }
        if (!component.IsEnabled && component.Overlay is { })
        {
            DestroyOverlay(component);
            return;
        }
    }

    private void HandleOverlayProgressUpdate(TextureFadeOverlayComponent component, float frameTime)
    {
        if (component.Overlay == null)
            return;
        if (component.ProgressSpeed != 0f)
        {
            component.FadeProgress += component.ProgressSpeed * frameTime;
            component.FadeProgress = Math.Clamp(component.FadeProgress, component.MinProgress, component.MaxProgress);
        }
        var fadeProgressMod = component.FadeProgress;
        fadeProgressMod += (float)Math.Sin(Math.PI * component.Overlay.Time.TotalSeconds * component.PulseRate) * component.PulseMagnitude;
        fadeProgressMod = Math.Clamp(fadeProgressMod, 0f, 1f);
        component.Overlay.FadeProgress = fadeProgressMod;
        component.Overlay.Modulate = component.Modulate;
        component.Overlay.ZIndex = component.ZIndex;
    }

    private TextureFadeOverlay CreateOverlay(TextureFadeOverlayComponent component)
    {
        var overlay = new TextureFadeOverlay()
        {
            Sprite = component.Sprite,
            Modulate = component.Modulate,
            ZIndex = component.ZIndex,
        };
        OverlayStack.Get(_overlayManager).AddOverlay(overlay);
        return overlay;
    }

    private void DestroyOverlay(TextureFadeOverlayComponent component)
    {
        if (component.Overlay is null)
            return;
        OverlayStack.Get(_overlayManager).RemoveOverlay(component.Overlay);
        component.Overlay.Dispose();
        component.Overlay = null;
    }
}
