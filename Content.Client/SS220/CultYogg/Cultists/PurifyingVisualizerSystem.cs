// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.Cultists;

/// <summary>
/// </summary>
public sealed class PurifyingVisualizerSystem : VisualizerSystem<CultYoggPurifiedComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggPurifiedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CultYoggPurifiedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<CultYoggPurifiedComponent> uid, ref ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(PurifyingVisualLayers.Particles, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void OnComponentInit(Entity<CultYoggPurifiedComponent> uid, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        sprite.LayerMapReserveBlank(PurifyingVisualLayers.Particles);
        sprite.LayerSetVisible(PurifyingVisualLayers.Particles, true);
        sprite.LayerSetShader(PurifyingVisualLayers.Particles, "unshaded");

        if (uid.Comp.Sprite != null)
        {
            sprite.LayerSetRSI(PurifyingVisualLayers.Particles, uid.Comp.Sprite.RsiPath);
            sprite.LayerSetState(PurifyingVisualLayers.Particles, uid.Comp.Sprite.RsiState);
        }
    }
}

public enum PurifyingVisualLayers : byte
{
    Particles
}
