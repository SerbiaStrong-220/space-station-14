// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.SS220.ThoughtBubble;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.SS220.ThoughtBubble;

/// <summary>
/// Handles thought bubble visuals - spawns, updates position/rotation
/// </summary>
public sealed class ThoughtBubbleSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private const float VerticalOffset = 0.8f;
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
        var item = GetEntity(ent.Comp.PointedItem);
        var mapCoord = _transform.GetMapCoordinates(ent.Owner);

        if (ent.Comp.BubbleEntity != null)
        {
            Del(ent.Comp.BubbleEntity);
            ent.Comp.BubbleEntity = null;
        }

        ent.Comp.BubbleEntity ??= Spawn(ent.Comp.BubbleProto, mapCoord);

        if (ent.Comp.BubbleEntity == null)
            return;

        if (!TryComp<SpriteComponent>(ent.Comp.BubbleEntity, out var thoughtSprite) ||
            !TryComp<SpriteComponent>(item, out var itemSprite))
            return;

        if (!_sprite.LayerMapTryGet((ent.Comp.BubbleEntity.Value, thoughtSprite),
                ThoughtBubbleVisuals.Icon,
                out var targetLayer,
                false))
            return;

        var rsiItem = _sprite.LayerGetEffectiveRsi((item, itemSprite), 0);
        var state = _sprite.LayerGetRsiState((item, itemSprite), 0);

        if (rsiItem == null || state == null)
            return;

        //Is this really a convenient way to transferring sprite data?
        //Possible display bugs due to multiple layers on the item.
        var layerData = new PrototypeLayerData
        {
            RsiPath = rsiItem.Path.ToString(),
            State = state.ToString(),
        };

        _sprite.LayerSetData((ent.Comp.BubbleEntity.Value, thoughtSprite), targetLayer, layerData);
        _sprite.LayerSetVisible((ent.Comp.BubbleEntity.Value, thoughtSprite), targetLayer, true);
    }

    private void OnShutdown(Entity<ThoughtBubbleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.BubbleEntity == null)
            return;

        QueueDel(ent.Comp.BubbleEntity);
        ent.Comp.BubbleEntity = null;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<ThoughtBubbleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.BubbleEntity == null)
                continue;

            var ownerWorldPos = _transform.GetWorldPosition(uid);

            var eyeAngle = _eyeManager.CurrentEye.Rotation;
            var eyeWorld = (-eyeAngle).ToWorldVec();

            var offset = eyeWorld * VerticalOffset;
            _transform.SetWorldPosition(comp.BubbleEntity.Value, ownerWorldPos - offset);

            var targetAngle = eyeWorld.ToAngle() + Angle.FromDegrees(90);
            _transform.SetWorldRotation(comp.BubbleEntity.Value, targetAngle);
        }
    }
}
