using Content.Shared.SS220.EntityEffects.Effects;
using Content.Shared.Temperature.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Temperature.Systems;

public sealed partial class EntityHeaterSystem : SharedEntityHeaterSystem
{
    //SS220-grill-update begin
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrillingVisualComponent, AfterAutoHandleStateEvent>(OnGrillingVisualAdd);
        SubscribeLocalEvent<GrillingVisualComponent, ComponentShutdown>(OnGrillingVisualRemoved);
    }

    private void OnGrillingVisualAdd(Entity<GrillingVisualComponent> ent, ref AfterAutoHandleStateEvent args)
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

    private void OnGrillingVisualRemoved(Entity<GrillingVisualComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            _spriteSystem.RemoveLayer((ent, sprite), GrillingLayer);
        }
    }

    //SS220-grill-update end
}
