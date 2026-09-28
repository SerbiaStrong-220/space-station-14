// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.Cultists;

/// <summary>
/// Controls the visuals during ascension to Mi-Go.
/// </summary>
public sealed partial class AscendingVisualizerSystem : VisualizerSystem<AscendingComponent>
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AscendingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AscendingComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<AscendingComponent> uid, ref ComponentShutdown args)
    {
        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), AscendingVisualLayers.Particles, out var layer, false))
        {
            _sprite.RemoveLayer((uid, sprite), layer);
        }
    }

    private void OnComponentInit(Entity<AscendingComponent> uid, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        _sprite.LayerMapReserve((uid, sprite), AscendingVisualLayers.Particles);
        _sprite.LayerSetVisible((uid, sprite), AscendingVisualLayers.Particles, true);
        sprite.LayerSetShader(AscendingVisualLayers.Particles, "unshaded");

        if (uid.Comp.Sprite == null)
            return;

        _sprite.LayerSetRsi((uid, sprite), AscendingVisualLayers.Particles, uid.Comp.Sprite.RsiPath);
        _sprite.LayerSetRsiState((uid, sprite), AscendingVisualLayers.Particles, uid.Comp.Sprite.RsiState);
    }
}

public enum AscendingVisualLayers : byte
{
    Particles
}
