// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.Cultists;

/// <summary>
/// </summary>
public sealed class AcsendingVisualizerSystem : VisualizerSystem<AcsendingComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AcsendingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AcsendingComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<AcsendingComponent> uid, ref ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(AcsendingVisualLayers.Particles, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void OnComponentInit(Entity<AcsendingComponent> uid, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        sprite.LayerMapReserveBlank(AcsendingVisualLayers.Particles);
        sprite.LayerSetVisible(AcsendingVisualLayers.Particles, true);
        sprite.LayerSetShader(AcsendingVisualLayers.Particles, "unshaded");

        if (uid.Comp.Sprite != null)
        {
            sprite.LayerSetRSI(AcsendingVisualLayers.Particles, uid.Comp.Sprite.RsiPath);
            sprite.LayerSetState(AcsendingVisualLayers.Particles, uid.Comp.Sprite.RsiState);
        }
    }
}

public enum AcsendingVisualLayers : byte
{
    Particles
}
