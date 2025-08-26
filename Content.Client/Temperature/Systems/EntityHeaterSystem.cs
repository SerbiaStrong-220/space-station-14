using Content.Shared.SS220.EntityEffects.Effects;
using Content.Shared.Temperature.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Temperature.Systems;

//SS220-grill-update This is actually a Client system, I don't know why it named this way
public sealed partial class EntityHeaterSystem : SharedEntityHeaterSystem
{
    //SS220-grill-update begin
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrillingVisualComponent, ComponentAdd>(OnGrillingVisualAdd);
        SubscribeLocalEvent<GrillingVisualComponent, ComponentRemove>(OnGrillingVisualRemoved);
    }

    private void OnGrillingVisualAdd(EntityUid ent, GrillingVisualComponent effect, ref ComponentAdd args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            var layer = new PrototypeLayerData
            {
                RsiPath = effect.GrillingSprite.RsiPath.ToString(),
                State = effect.GrillingSprite.RsiState,
                MapKeys = [effect.GrillingLayer]
            };

            _spriteSystem.AddLayer((ent, sprite), layer, null);
        }
    }

    private void OnGrillingVisualRemoved(EntityUid ent, GrillingVisualComponent effect, ref ComponentRemove args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            _spriteSystem.RemoveLayer((ent, sprite), effect.GrillingLayer);
        }
    }

    //SS220-grill-update end
}
