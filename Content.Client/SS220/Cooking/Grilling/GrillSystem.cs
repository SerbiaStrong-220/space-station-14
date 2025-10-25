// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.EntityEffects.Effects;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Cooking.Grilling;

/// <summary>
/// This handles all grill related visuals
/// </summary>
public sealed class GrillSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    private const string GrillingLayer = "grilling-layer";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrillingVisualComponent, AfterAutoHandleStateEvent>(AddGrillVisuals);
        SubscribeLocalEvent<GrillingVisualComponent, ComponentShutdown>(RemoveGrillVisuals);
    }

    private void AddGrillVisuals(Entity<GrillingVisualComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var layerData = new PrototypeLayerData()
        {
            RsiPath = ent.Comp.GrillingSprite?.RsiPath.ToString(),
            State = ent.Comp.GrillingSprite?.RsiState,
            MapKeys = [GrillingLayer]
        };

        if (_spriteSystem.TryGetLayer((ent.Owner, sprite), GrillingLayer, out var layer, false))
            _spriteSystem.LayerSetData(layer, layerData);
        else
            _spriteSystem.AddLayer((ent.Owner, sprite), layerData, null);
    }

    private void RemoveGrillVisuals(Entity<GrillingVisualComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            if(_spriteSystem.LayerExists((ent.Owner, sprite), GrillingLayer))
            {
                _spriteSystem.RemoveLayer((ent, sprite), GrillingLayer);
            }
        }
    }
}
