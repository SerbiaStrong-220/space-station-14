// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CultYogg.Cultists;

/// <summary>
/// Сontrols the visual during the acsending to the Mi-Go.
/// </summary>
public sealed class AcsendingVisualizerSystem : VisualizerSystem<AcsendingComponent>
{

    [Dependency] private readonly SpriteSystem _sprite = default!;

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
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((uid, sprite), AcsendingVisualLayers.Particles, out var layer, false))
        {
            _sprite.RemoveLayer((uid, sprite), layer);
        }
    }

    private void OnComponentInit(Entity<AcsendingComponent> uid, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        _sprite.LayerMapReserve((uid, sprite), AcsendingVisualLayers.Particles);
        _sprite.LayerSetVisible((uid, sprite), AcsendingVisualLayers.Particles, true);
        sprite.LayerSetShader(AcsendingVisualLayers.Particles, "unshaded");

        if (uid.Comp.Sprite == null)
            return;

        _sprite.LayerSetRsi((uid, sprite), AcsendingVisualLayers.Particles, uid.Comp.Sprite.RsiPath);
        _sprite.LayerSetRsiState((uid, sprite), AcsendingVisualLayers.Particles, uid.Comp.Sprite.RsiState);
    }
}

public enum AcsendingVisualLayers : byte
{
    Particles
}
