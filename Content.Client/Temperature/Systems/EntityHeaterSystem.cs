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

    private void OnGrillingVisualAdd(EntityUid ent, GrillingVisualComponent effect, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            var layer = new PrototypeLayerData
            {
                RsiPath = effect.GrillingSprite.RsiPath.ToString(),
                State = effect.GrillingSprite.RsiState,
                MapKeys = [GrillingLayer]
            };

            _spriteSystem.AddLayer((ent, sprite), layer, null);
        }
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
