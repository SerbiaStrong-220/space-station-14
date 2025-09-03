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

        SubscribeNetworkEvent<HeaterVisualsEvent>(OnGrillingVisualAdd);
        SubscribeLocalEvent<GrillingVisualComponent, ComponentRemove>(OnGrillingVisualRemoved);
    }

    private void OnGrillingVisualAdd(HeaterVisualsEvent ev)
    {
        var grillingEntity = GetEntity(ev.Target);

        if (TryComp<SpriteComponent>(grillingEntity, out var sprite))
        {
            var layer = new PrototypeLayerData
            {
                RsiPath = ev.GrillingSprite.RsiPath.ToString(),
                State = ev.GrillingSprite.RsiState,
                MapKeys = [GrillingLayer]
            };

            _spriteSystem.AddLayer((grillingEntity, sprite), layer, null);
        }
    }

    private void OnGrillingVisualRemoved(EntityUid ent, GrillingVisualComponent effect, ref ComponentRemove args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            _spriteSystem.RemoveLayer((ent, sprite), GrillingLayer);
        }
    }

    //SS220-grill-update end
}
