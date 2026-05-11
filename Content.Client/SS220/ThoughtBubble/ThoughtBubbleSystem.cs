// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.SS220.ThoughtBubble;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.ThoughtBubble;

/// <summary>
/// Handles thought bubble visuals - spawns, updates position/rotation
/// </summary>
public sealed class ThoughtBubbleSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private const float ItemSpriteScale = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThoughtBubbleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ThoughtBubbleComponent, AfterAutoHandleStateEvent>(OnStateHandled);
        SubscribeLocalEvent<ThoughtBubbleComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnComponentInit(Entity<ThoughtBubbleComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerMapReserve((ent, sprite), ThoughtBubbleVisuals.Icon);
        _sprite.LayerSetVisible((ent, sprite), ThoughtBubbleVisuals.Icon, false);
    }

    private void OnStateHandled(Entity<ThoughtBubbleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (GetEntity(ent.Comp.PointedItem) is not { Valid: true } item)
            return;

        if (ent.Comp.ShownInBubbleItem == item)
            return;

        PredictedQueueDel(ent.Comp.BubbleEntity);
        if (!TryComp<SpriteComponent>(item, out var itemSprite))
            return;

        var thought = SpawnAttachedTo(ent.Comp.BubbleProto, _transform.ToCoordinates(ent.Owner, _transform.GetMapCoordinates(ent.Owner)));
        ent.Comp.BubbleEntity = thought;

        if (!TryComp<SpriteComponent>(thought, out var thoughtSprite))
            return;

        if (TryComp<SpriteComponent>(ent.Owner, out var ownerSprite) && ownerSprite.DrawDepth > thoughtSprite.DrawDepth)
            // +1 to draw over owner
            _sprite.SetDrawDepth((thought, thoughtSprite), ownerSprite.DrawDepth + 1);

        if (!_sprite.LayerMapTryGet((thought, thoughtSprite),
            ThoughtBubbleVisuals.Icon, out var targetIndexLayer, logMissing: false))
            return;

        // TODO remove
        _sprite.LayerSetVisible((thought, thoughtSprite), targetIndexLayer, false);

        foreach (var layer in itemSprite.AllLayers)
        {
            if (layer is not SpriteComponent.Layer spriteLayer)
                continue;

            var protoData = spriteLayer.ToPrototypeData();
            protoData.RsiPath ??= spriteLayer.ActualRsi?.Path.CanonPath;
            protoData.Scale *= ItemSpriteScale;

            _sprite.AddLayer((thought, thoughtSprite), protoData, 1);
        }
    }

    private void OnShutdown(Entity<ThoughtBubbleComponent> ent, ref ComponentShutdown args)
    {
        PredictedQueueDel(ent.Comp.BubbleEntity);
        ent.Comp.BubbleEntity = null;
    }
}
