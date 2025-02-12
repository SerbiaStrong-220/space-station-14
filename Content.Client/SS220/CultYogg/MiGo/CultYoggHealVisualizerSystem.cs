// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.MiGo;

/// <summary>
/// </summary>
public sealed class CultYoggHealVisualizerSystem : VisualizerSystem<CultYoggHealComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggHealComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CultYoggHealComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<CultYoggHealComponent> uid, ref ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(HealVisualLayers.Particles, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void OnComponentInit(Entity<CultYoggHealComponent> uid, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        sprite.LayerMapReserveBlank(HealVisualLayers.Particles);
        sprite.LayerSetVisible(HealVisualLayers.Particles, true);
        sprite.LayerSetShader(HealVisualLayers.Particles, "unshaded");

        if (uid.Comp.Sprite != null)
        {
            sprite.LayerSetRSI(HealVisualLayers.Particles, uid.Comp.Sprite.RsiPath);
            sprite.LayerSetState(HealVisualLayers.Particles, uid.Comp.Sprite.RsiState);
        }
    }
}

public enum HealVisualLayers : byte
{
    Particles
}
