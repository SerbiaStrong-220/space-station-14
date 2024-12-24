using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.Cultists;

/// <summary>
/// </summary>
public sealed class CleansingVisualizerSystem : VisualizerSystem<CultYoggCleansedComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggCleansedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CultYoggCleansedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<CultYoggCleansedComponent> uid, ref ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(CleansingVisualLayers.Particles, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void OnComponentInit(Entity<CultYoggCleansedComponent> uid, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        sprite.LayerMapReserveBlank(CleansingVisualLayers.Particles);
        sprite.LayerSetVisible(CleansingVisualLayers.Particles, true);
        sprite.LayerSetShader(CleansingVisualLayers.Particles, "unshaded");

        if (uid.Comp.Sprite != null)
        {
            sprite.LayerSetRSI(CleansingVisualLayers.Particles, uid.Comp.Sprite.RsiPath);
            sprite.LayerSetState(CleansingVisualLayers.Particles, uid.Comp.Sprite.RsiState);
        }
    }
}

public enum CleansingVisualLayers : byte
{
    Particles
}
