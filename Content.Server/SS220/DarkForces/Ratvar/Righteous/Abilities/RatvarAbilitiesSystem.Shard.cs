using System.Numerics;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment.Items;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Altar;
using Content.Server.Mind;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Items;
using Content.Shared.SS220.DarkForces.Ratvar.UI;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities;

public sealed partial class RatvarAbilitiesSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly RatvarProgressSystem _progressSystem = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string TileConvertEffect = "RatvarTileSpawnEffect";

    [ValidatePrototypeId<EntityPrototype>]
    private const string WallConvertEffect = "RatvarWallSpawnEffect";

    [ValidatePrototypeId<EntityPrototype>]
    private const string RatvarCyborg = "MobRatvarCyborg";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ClockWall = "WallClock";

    private void InitializeShard()
    {
        SubscribeLocalEvent<RatvarShardComponent, RatvarShardReconstructEvent>(OnReconstructEvent);
        SubscribeLocalEvent<RatvarShardComponent, RatvarShardEmpEvent>(OnEmpEvent);
        SubscribeLocalEvent<RatvarShardComponent, RatvarEnchantmentSelectedMessage>(OnEnchantmentSelected);
        SubscribeLocalEvent<RatvarShardComponent, AfterInteractEvent>(OnShardAfterInteract);
    }

    private void OnShardAfterInteract(EntityUid uid, RatvarShardComponent component, AfterInteractEvent args)
    {
        if (!HasComp<RatvarAltarComponent>(args.Target) || !HasComp<HumanoidAppearanceComponent>(args.User))
            return;

        if (!_progressSystem.IsEntityAtSummonPoint(args.Used) || _progressSystem.IsPortalInProgress())
            return;

        var transform = Transform(uid);
        Spawn("RatvarPortal", transform.Coordinates);

        QueueDel(args.Target);
        QueueDel(uid);
    }

    private void OnReconstructEvent(EntityUid uid, RatvarShardComponent component, RatvarShardReconstructEvent args)
    {
        if (args.Handled)
            return;

        ConvertBorgs(uid);
        ConvertNearblyTiles(uid, component);
        ConvertNearblyWalls(uid, component);

        QueueDel(uid);
        args.Handled = true;
    }

    private void ConvertBorgs(EntityUid uid)
    {
        var transform = Transform(uid);
        var borgs = _entityLookupSystem.GetEntitiesInRange<BorgChassisComponent>(transform.Coordinates, 5f);
        foreach (var borg in borgs)
        {
            ConvertBorg(borg);
        }
    }

    public void ConvertBorg(EntityUid borg)
    {
        var transform = Transform(borg);
        if (!_mindSystem.TryGetMind(borg, out var mindId, out _))
            return;

        var ratvarCyborg = Spawn(RatvarCyborg, transform.Coordinates);
        _mindSystem.TransferTo(mindId, ratvarCyborg);

        QueueDel(borg);
    }

    private void ConvertNearblyTiles(EntityUid uid, RatvarShardComponent component)
    {
        var shardTransform = Transform(uid);
        var targetUid = Transform(shardTransform.ParentUid);
        var gridUid = targetUid.GridUid;
        var pos = targetUid.Coordinates;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var range = component.ConvertRange;
        var tilesRefs = grid.GetLocalTilesIntersecting(new Box2(pos.Position + new Vector2(-range, -range),
            pos.Position + new Vector2(range, range)));

        var cultTileDef = (ContentTileDefinition) _tileDefinition[component.TileId];
        var cultTile = new Tile(cultTileDef.TileId);

        foreach (var tile in tilesRefs)
        {
            var tilePos = _turf.GetTileCenter(tile);
            if (!pos.InRange(EntityManager, tilePos, range))
                continue;

            if (tile.Tile.TypeId == cultTile.TypeId)
                continue;

            _tile.ReplaceTile(tile, cultTileDef);
            Spawn(TileConvertEffect, tilePos);
        }
    }

    private void ConvertNearblyWalls(EntityUid uid, RatvarShardComponent component)
    {
        var sharedTransform = Transform(uid);
        var targetTransform = Transform(sharedTransform.ParentUid);
        var pos = targetTransform.Coordinates;

        var range = component.ConvertRange;
        var entitiesInRange = _entityLookupSystem.GetEntitiesInRange<TagComponent>(pos, range);

        foreach (var entity in entitiesInRange)
        {
            if (!_tag.HasTag(entity.Owner, "Wall"))
                continue;

            var transform = Transform(entity);
            Spawn(ClockWall, transform.Coordinates);
            Spawn(WallConvertEffect, transform.Coordinates);

            QueueDel(entity);
        }
    }

    private void OnEmpEvent(EntityUid uid, RatvarShardComponent component, RatvarShardEmpEvent args)
    {
        if (args.Handled)
            return;

        var mapCoordinates = _transformSystem.GetMapCoordinates(uid);

        _empSystem.EmpPulse(mapCoordinates, 5f, 20000, 10);
        QueueDel(uid);
        args.Handled = true;
    }

    private void OnEnchantmentSelected(EntityUid uid, RatvarShardComponent component,
        RatvarEnchantmentSelectedMessage args)
    {
        if (Enum.TryParse<RatvarShardOverlays>(args.Visuals, out var visuals))
            _appearance.SetData(uid, RatvarEnchantmentableVisuals.State, visuals);
    }
}
