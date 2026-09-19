using System.Numerics;
using Content.Shared.SS220.Felinid.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.SS220.Felinid;

public sealed partial class DisposalPipeCrawlerOverlay : Overlay
{
    private const string PipeLayer = "pipe";
    private const int OverlayZIndex = 100;

    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;

    private EntityLookupSystem _lookup;
    private SpriteSystem _sprite;
    private TransformSystem _transform;
    private readonly HashSet<Entity<DisposalPipeCrawlerTubeComponent>> _nearbyTubes = new();
    private readonly HashSet<Entity<DisposalPipeCrawlerContentsComponent>> _nearbyContents = new();
    private readonly HashSet<Entity<DisposalPipeCrawlerComponent>> _nearbyCrawlers = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public DisposalPipeCrawlerOverlay()
    {
        IoCManager.InjectDependencies(this);
        _lookup = _entity.System<EntityLookupSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();
        ZIndex = OverlayZIndex;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player ||
            !_entity.TryGetComponent<DisposalPipeCrawlerComponent>(player, out var pipecrawl) ||
            !pipecrawl.InsidePipe ||
            !_entity.TryGetComponent<EyeComponent>(player, out var eye) ||
            args.Viewport.Eye != eye.Eye ||
            !_entity.TryGetComponent<TransformComponent>(player, out var playerXform))
        {
            return;
        }

        var revealRangeSquared = pipecrawl.VisionRange * pipecrawl.VisionRange;
        var playerPosition = _transform.GetWorldPosition(playerXform);
        var mapId = playerXform.MapID;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var handle = args.WorldHandle;

        _nearbyTubes.Clear();
        _lookup.GetEntitiesInRange(
            playerXform.Coordinates,
            pipecrawl.VisionRange,
            _nearbyTubes,
            LookupFlags.Static | LookupFlags.Sundries | LookupFlags.Approximate);

        foreach (var (uid, _) in _nearbyTubes)
        {
            if (!_entity.TryGetComponent<SpriteComponent>(uid, out var sprite) ||
                !_entity.TryGetComponent<TransformComponent>(uid, out var xform))
            {
                continue;
            }

            RenderIfNearby(
                (uid, sprite, xform),
                mapId,
                playerPosition,
                revealRangeSquared,
                eyeRotation,
                handle,
                revealPipeLayer: true);
        }

        _nearbyContents.Clear();
        _lookup.GetEntitiesInRange(
            playerXform.Coordinates,
            pipecrawl.VisionRange,
            _nearbyContents);
        foreach (var (uid, _) in _nearbyContents)
        {
            if (!_entity.TryGetComponent<SpriteComponent>(uid, out var sprite) ||
                !_entity.TryGetComponent<TransformComponent>(uid, out var xform))
            {
                continue;
            }

            RenderIfNearby((uid, sprite, xform), mapId, playerPosition, revealRangeSquared, eyeRotation, handle);
        }

        _nearbyCrawlers.Clear();
        _lookup.GetEntitiesInRange(
            playerXform.Coordinates,
            pipecrawl.VisionRange,
            _nearbyCrawlers);
        foreach (var (uid, otherPipecrawl) in _nearbyCrawlers)
        {
            if (uid == player || !otherPipecrawl.InsidePipe)
                continue;

            if (!_entity.TryGetComponent<SpriteComponent>(uid, out var sprite) ||
                !_entity.TryGetComponent<TransformComponent>(uid, out var xform))
            {
                continue;
            }

            RenderIfNearby((uid, sprite, xform), mapId, playerPosition, revealRangeSquared, eyeRotation, handle);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void RenderIfNearby(
        Entity<SpriteComponent, TransformComponent> ent,
        MapId mapId,
        Vector2 playerPosition,
        float revealRangeSquared,
        Angle eyeRotation,
        DrawingHandleWorld handle,
        bool revealPipeLayer = false)
    {
        if (ent.Comp2.MapID != mapId)
            return;

        var position = _transform.GetWorldPosition(ent.Comp2);
        if (Vector2.DistanceSquared(playerPosition, position) > revealRangeSquared)
            return;

        var rotation = _transform.GetWorldRotation(ent.Comp2);
        if (revealPipeLayer &&
            _sprite.LayerMapTryGet((ent.Owner, ent.Comp1), PipeLayer, out var pipeLayer, false))
        {
            var wasVisible = ent.Comp1[pipeLayer].Visible;
            try
            {
                ent.Comp1[pipeLayer].Visible = true;
                _sprite.RenderSprite((ent.Owner, ent.Comp1), handle, eyeRotation, rotation, position);
            }
            finally
            {
                ent.Comp1[pipeLayer].Visible = wasVisible;
            }

            return;
        }

        _sprite.RenderSprite((ent.Owner, ent.Comp1), handle, eyeRotation, rotation, position);
    }
}
