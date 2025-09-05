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

        var exist = _spriteSystem.TryGetLayer((ent.Owner, sprite), GrillingLayer, out var layer);

        layer ??= new PrototypeLayerData();
        layer.RsiPath = ent.Comp.GrillingSprite.RsiPath.ToString();
        layer.State = ent.Comp.GrillingSprite.RsiState;
        layer.MapKeys = [GrillingLayer];

        if (!exist)
            _spriteSystem.AddLayer((ent.Owner, sprite), layer, null);
    }

    private void OnGrillingVisualRemoved(EntityUid ent, GrillingVisualComponent effect, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            _spriteSystem.RemoveLayer((ent, sprite), GrillingLayer);
        }
    }

    //SS220-grill-update end
}
