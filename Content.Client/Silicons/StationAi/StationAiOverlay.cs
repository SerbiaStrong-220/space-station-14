using System.Numerics;
using Content.Client.Graphics;
using Content.Shared.Changeling.Mutations; // SS220 Changeling digital camouflage
using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects; // SS220 Changeling digital camouflage
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map; // SS220 Changeling digital camouflage
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CameraStaticShader = "CameraStatic";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDrawShader = "StencilDraw";

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();

    private readonly OverlayResourceCache<CachedResources> _resources = new();

    // SS220 Changeling digital camouflage begin
    private readonly ContainerSystem _containerSystem;
    private readonly EntityLookupSystem _entityLookup;
    private readonly SharedMapSystem _mapSystem;
    // SS220 Changeling digital camouflage end

    private float _updateRate = 1f / 30f;
    private float _accumulator;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);

        // SS220 Changeling digital camouflage begin
        _containerSystem = _entManager.System<ContainerSystem>();
        _entityLookup = _entManager.System<EntityLookupSystem>();
        _mapSystem = _entManager.System<SharedMapSystem>();
        // SS220 Changeling digital camouflage end
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var res = _resources.GetForViewport(args.Viewport, static _ => new CachedResources());

        if (res.StencilTexture?.Texture.Size != args.Viewport.Size)
        {
            res.StaticTexture?.Dispose();
            res.StencilTexture?.Dispose();
            res.StencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "station-ai-stencil");
            res.StaticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "station-ai-static");
        }

        var worldHandle = args.WorldHandle;

        var worldBounds = args.WorldBounds;

        var playerEnt = _player.LocalEntity;
        _entManager.TryGetComponent(playerEnt, out TransformComponent? playerXform);
        var gridUid = playerXform?.GridUid ?? EntityUid.Invalid;
        _entManager.TryGetComponent(gridUid, out MapGridComponent? grid);
        _entManager.TryGetComponent(gridUid, out BroadphaseComponent? broadphase);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        _accumulator -= (float) _timing.FrameTime.TotalSeconds;

        if (grid != null && broadphase != null)
        {
            var lookups = _entManager.System<EntityLookupSystem>();
            var xforms = _entManager.System<SharedTransformSystem>();

            if (_accumulator <= 0f)
            {
                _accumulator = MathF.Max(0f, _accumulator + _updateRate);
                _visibleTiles.Clear();
                _entManager.System<StationAiVisionSystem>().GetView((gridUid, broadphase, grid), worldBounds, _visibleTiles);
            }

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty =  Matrix3x2.Multiply(gridMatrix, invMatrix);

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(res.StencilTexture!, () =>
            {
                worldHandle.SetTransform(matty);

                foreach (var tile in _visibleTiles)
                {
                    var aabb = lookups.GetLocalBounds(tile, grid.TileSize);
                    worldHandle.DrawRect(aabb, Color.White);
                }
            },
            Color.Transparent);

            // Once this is gucci optimise rendering.
            worldHandle.RenderInRenderTarget(res.StaticTexture!,
            () =>
            {
                worldHandle.SetTransform(invMatrix);
                var shader = _proto.Index(CameraStaticShader).Instance();
                worldHandle.UseShader(shader);
                worldHandle.DrawRect(worldBounds, Color.White);
            },
            Color.Black);
        }
        // Not on a grid
        else
        {
            worldHandle.RenderInRenderTarget(res.StencilTexture!, () =>
            {
            },
            Color.Transparent);

            worldHandle.RenderInRenderTarget(res.StaticTexture!,
            () =>
            {
                worldHandle.SetTransform(Matrix3x2.Identity);
                worldHandle.DrawRect(worldBounds, Color.Black);
            }, Color.Black);
        }

        // Use the lighting as a mask
        worldHandle.UseShader(_proto.Index(StencilMaskShader).Instance());
        worldHandle.DrawTextureRect(res.StencilTexture!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index(StencilDrawShader).Instance());
        worldHandle.DrawTextureRect(res.StaticTexture!.Texture, worldBounds);

        // SS220 Changeling digital camouflage begin
        if (grid != null && broadphase != null)
            DrawDigitalCamouflage(args, invMatrix, (gridUid, grid));
        // SS220 Changeling digital camouflage end

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);

    }

    // SS220 Changeling digital camouflage begin
    private void DrawDigitalCamouflage(
        in OverlayDrawArgs args,
        Matrix3x2 inverseMatrix,
        Entity<MapGridComponent> grid)
    {
        if (_player.LocalEntity is not { } player ||
            !_entManager.TryGetComponent<StationAiDigitalCamouflageComponent>(player, out var camouflage))
        {
            return;
        }

        var worldHandle = args.WorldHandle;
        worldHandle.SetTransform(inverseMatrix);
        worldHandle.UseShader(_proto.Index(CameraStaticShader).Instance());

        foreach (var netEntity in camouflage.CamouflagedEntities)
        {
            if (!_entManager.TryGetEntity(netEntity, out var uid) ||
                _entManager.Deleted(uid) ||
                !_entManager.TryGetComponent(uid, out TransformComponent? xform) ||
                xform.MapID == MapId.Nullspace ||
                xform.MapID != args.MapId ||
                xform.GridUid != grid.Owner ||
                !_visibleTiles.Contains(_mapSystem.LocalToTile(grid.Owner, grid.Comp, xform.Coordinates)) ||
                _containerSystem.IsEntityOrParentInContainer(uid.Value, xform: xform))
            {
                continue;
            }

            var bounds = _entityLookup.GetWorldAABB(uid.Value, xform).Enlarged(0.15f);
            if (bounds.Intersects(in args.WorldAABB))
                worldHandle.DrawRect(bounds, Color.White);
        }
    }
    // SS220 Changeling digital camouflage end

    protected override void DisposeBehavior()
    {
        _resources.Dispose();

        base.DisposeBehavior();
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? StaticTexture;
        public IRenderTexture? StencilTexture;

        public void Dispose()
        {
            StaticTexture?.Dispose();
            StencilTexture?.Dispose();
        }
    }
}
