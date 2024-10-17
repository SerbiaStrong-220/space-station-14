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
        DeinitializeLayers(entity.Comp);
    }

    private void HandleOverlayActivityUpdate(TextureFadeOverlayComponent component)
    {
        if (component.IsEnabled && !component.IsOverlayInitialized)
        {
            InitializeLayers(component);
            return;
        }
        if (!component.IsEnabled && component.IsOverlayInitialized)
        {
            DeinitializeLayers(component);
            return;
        }
    }

    private void HandleOverlayProgressUpdate(TextureFadeOverlayComponent component, float frameTime)
    {
        if (!component.IsOverlayInitialized)
            return;
        for (var i = 0; i < component.Layers.Count; i++)
        {
            var layer = component.Layers[i];
            if (layer.Overlay is null)
                continue;
            if (layer.ProgressSpeed != 0f)
            {
                layer.FadeProgress += layer.ProgressSpeed * frameTime;
                layer.FadeProgress = Math.Clamp(layer.FadeProgress, layer.MinProgress, layer.MaxProgress);
                component.Layers[i] = layer;
            }
            var fadeProgressMod = layer.FadeProgress;
            fadeProgressMod += (float)Math.Sin(Math.PI * layer.Overlay.Time.TotalSeconds * layer.PulseRate) * layer.PulseMagnitude;
            fadeProgressMod = Math.Clamp(fadeProgressMod, 0f, 1f);
            layer.Overlay.FadeProgress = fadeProgressMod;
            layer.Overlay.Modulate = layer.Modulate;
            layer.Overlay.ZIndex = layer.ZIndex;
        }
    }

    private void InitializeLayers(TextureFadeOverlayComponent component)
    {
        if (component.IsOverlayInitialized)
            return;
        component.IsOverlayInitialized = true;
        for (var i = 0; i < component.Layers.Count; i++)
        {
            var layer = component.Layers[i];
            layer.Overlay = new TextureFadeOverlay()
            {
                Sprite = layer.Sprite,
                Modulate = layer.Modulate,
                ZIndex = layer.ZIndex,
            };
            OverlayStack.Get(_overlayManager).AddOverlay(layer.Overlay);
            component.Layers[i] = layer;
        }
    }

    private void DeinitializeLayers(TextureFadeOverlayComponent component)
    {
        if (!component.IsOverlayInitialized)
            return;
        component.IsOverlayInitialized = false;
        for (var i = 0; i < component.Layers.Count; i++)
        {
            var layer = component.Layers[i];
            var overlay = layer.Overlay;
            if (overlay is null)
                continue;
            OverlayStack.Get(_overlayManager).RemoveOverlay(overlay);
            overlay.Dispose();
            layer.Overlay = null;
            component.Layers[i] = layer;
        }
    }
}
