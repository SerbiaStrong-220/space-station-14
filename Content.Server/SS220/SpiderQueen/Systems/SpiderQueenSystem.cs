// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.SS220.SpiderQueen.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.SS220.SpiderQueen;
using Content.Shared.SS220.SpiderQueen.Components;
using Content.Shared.SS220.SpiderQueen.Systems;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Tiles;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace Content.Server.SS220.SpiderQueen.Systems;

public sealed partial class SpiderQueenSystem : SharedSpiderQueenSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    private readonly HashSet<EntityUid> _turfCheck = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderQueenComponent, AfterCocooningEvent>(OnAfterCocooning);

        SubscribeLocalEvent<SpiderTargetSpawnEvent>(OnTargetSpawn);
        SubscribeLocalEvent<SpiderNearbySpawnEvent>(OnNearbySpawn);
        SubscribeLocalEvent<SpiderSpawnDoAfterEvent>(OnSpawnDoAfter);

        SubscribeLocalEvent<SpiderTileSpawnActionEvent>(OnTileSpawnAction);
        SubscribeLocalEvent<SpiderTileSpawnDoAfterEvent>(OnTileSpawnDoAfter);
    }
    private static readonly Vector2 CheckRange = new(1f, 1f);
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpiderQueenComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextSecond)
                continue;

            comp.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (!_hunger.IsHungerBelowState(uid, HungerThreshold.Okay))
                ConvertHungerIntoBloodPoints(uid, comp, comp.HungerConversionPerSecond);
        }
    }

    private void OnTargetSpawn(SpiderTargetSpawnEvent args)
    {
        var performer = args.Performer;
        if (args.Handled ||
            !CheckEnoughBloodPoints(performer, args.Cost))
            return;

        if (TryStartSpiderSpawnDoAfter(performer, args.DoAfter, args.Target, args.Prototypes, args.Offset, args.SnapToGrid, args.Cost))
        {
            args.Handled = true;
        }
        else
        {
            Log.Error($"Failed to start DoAfter by {performer}");
            return;
        }
    }

    private void OnNearbySpawn(SpiderNearbySpawnEvent args)
    {
        var performer = args.Performer;
        if (args.Handled ||
            !TryComp<TransformComponent>(performer, out var transform) ||
            !CheckEnoughBloodPoints(performer, args.Cost))
            return;

        if (TryStartSpiderSpawnDoAfter(performer, args.DoAfter, transform.Coordinates, args.Prototypes, args.Offset, args.SnapToGrid, args.Cost))
        {
            args.Handled = true;
        }
        else
        {
            Log.Error($"Failed to start DoAfter by {performer}");
            return;
        }
    }

    private void OnSpawnDoAfter(SpiderSpawnDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled ||
            !CheckEnoughBloodPoints(user, args.Cost))
            return;

        var getProtos = EntitySpawnCollection.GetSpawns(args.Prototypes, _random);
        var targetMapCords = GetCoordinates(args.TargetCoordinates);
        if (args.SnapToGrid)
            targetMapCords.SnapToGrid(EntityManager, _mapManager);

        foreach (var proto in getProtos)
        {
            var ent = Spawn(proto, targetMapCords);
            targetMapCords = targetMapCords.Offset(args.Offset);

            if (TryComp<SpiderEggComponent>(ent, out var spiderEgg))
                spiderEgg.EggOwner = user;
        }

        if (TryComp<SpiderQueenComponent>(user, out var spiderQueen))
            ChangeBloodPointsAmount(user, spiderQueen, -args.Cost);
    }

    private void OnAfterCocooning(Entity<SpiderQueenComponent> entity, ref AfterCocooningEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!TryComp<TransformComponent>(target, out var transform) || !_mobState.IsDead(target))
            return;

        var targetCords = _transform.GetMoverCoordinates(target, transform);
        var cocoonPrototypeID = _random.Pick(entity.Comp.CocoonPrototypes);
        var cocoonUid = Spawn(cocoonPrototypeID, targetCords);

        if (!TryComp<SpiderCocoonComponent>(cocoonUid, out var spiderCocoon) ||
            !_container.TryGetContainer(cocoonUid, spiderCocoon.CocoonContainerId, out var container))
        {
            Log.Error($"{cocoonUid} doesn't have required components to cocooning target");
            return;
        }

        _container.Insert(target, container);
        entity.Comp.CocoonsList.Add(cocoonUid);
        entity.Comp.MaxBloodPoints += spiderCocoon.BloodPointsBonus;
        Dirty(entity);
        UpdateAlert(entity);

        spiderCocoon.CocoonOwner = entity.Owner;
        Dirty(cocoonUid, spiderCocoon);

        if (entity.Comp.CocoonsCountToAnnouncement is { } value &&
            entity.Comp.CocoonsList.Count >= value)
            DoStationAnnouncement(entity);
    }

    private void OnTileSpawnAction(SpiderTileSpawnActionEvent args)
    {
        var performer = args.Performer;
        if (args.Handled ||
            !CheckEnoughBloodPoints(performer, args.Cost))
            return;

        var netCoordinates = GetNetCoordinates(args.Target);
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            args.DoAfter,
            new SpiderTileSpawnDoAfterEvent()
            {
                Prototype = args.Prototype,
                TargetCoordinates = netCoordinates,
                Cost = args.Cost,
            },
            null
        )
        {
            Broadcast = true,
            BreakOnDamage = false,
            BreakOnMove = true,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            args.Handled = true;
    }


    private void OnTileSpawnDoAfter(SpiderTileSpawnDoAfterEvent args)
    {
        if (args.Cancelled ||
            !CheckEnoughBloodPoints(args.User, args.Cost))
        {
            return;
        }
        var coordinates = GetCoordinates(args.TargetCoordinates);
        var gridUid = _transform.GetGrid(coordinates);
        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            OnGridNotFound(args);
            return;
        }
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, coordinates);

        _mapSystem.SetTile(gridUid.Value,
            mapGrid,
            position,
            new Tile(_tileDefinitionManager[args.Prototype].TileId));

        if (TryComp<SpiderQueenComponent>(args.User, out var spiderQueen))
            ChangeBloodPointsAmount(args.User, spiderQueen, -args.Cost);
    }

    //basically ctrl+c,ctrl+v from FloorTileSystem.cs
    private void OnGridNotFound(SpiderTileSpawnDoAfterEvent args)
    {
        if (args.Handled)
        {
            return;
        }
        _entManager.TryGetEntity(args.TargetCoordinates.NetEntity, out var EntLocal);
        if (EntLocal == null)
        {
            return;
        }
        var location = new EntityCoordinates((EntityUid)EntLocal, args.TargetCoordinates.Position).AlignWithClosestGridTile();
        var locationMap = _transform.ToMapCoordinates(location);
        if (locationMap.MapId == MapId.Nullspace)
        {
            return;
        }

        var physicQuery = GetEntityQuery<PhysicsComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();

        var map = _transform.ToMapCoordinates(location);
        const bool inRange = true;
        var state = (inRange, location.EntityId);
        _mapManager.FindGridsIntersecting(map.MapId, new Box2(map.Position - CheckRange, map.Position + CheckRange), ref state,
            static (EntityUid entityUid, MapGridComponent grid, ref (bool weh, EntityUid EntityId) tuple) =>
            {
                if (tuple.EntityId == entityUid)
                    return true;

                tuple.weh = false;

                return false;
            });

        if (!state.inRange)
        {
            return;
        }
        var userPos = _transform.ToMapCoordinates(transformQuery.GetComponent(args.User).Coordinates).Position;
        var dir = userPos - map.Position;
        var canAccessCenter = false;
        if (dir.LengthSquared() > 0.01)
        {
            var ray = new CollisionRay(map.Position, dir.Normalized(), (int)CollisionGroup.Impassable);
            var results = _physics.IntersectRay(locationMap.MapId, ray, dir.Length(), returnOnFirstHit: true);
            canAccessCenter = !results.Any();
        }

        // if user can access tile center then they can place floor
        // otherwise check it isn't blocked by a wall
        if (!canAccessCenter && _turf.TryGetTileRef(location, out var tileRef))
        {
            _turfCheck.Clear();
            _lookup.GetEntitiesInTile(tileRef.Value, _turfCheck);
            foreach (var ent in _turfCheck)
            {
                if (physicQuery.TryGetComponent(ent, out var phys) &&
                    phys.BodyType == BodyType.Static &&
                    phys.Hard &&
                    (phys.CollisionLayer & (int)CollisionGroup.Impassable) != 0)
                {
                    return;
                }
            }
        }
        TryComp<MapGridComponent>(location.EntityId, out var mapGrid);

        var currentTileDefinition = (ContentTileDefinition)_tileDefinitionManager[args.Prototype];
        //var currentTileDefinition = (ContentTileDefinition)_tileDefinitionManager[new ProtoId < ContentTileDefinition > ("Space")];
        if (mapGrid != null)
        {
            var gridUid = location.EntityId;
            var tile = _mapSystem.GetTileRef(gridUid, mapGrid, location);

            if (!CanPlaceTile(gridUid, mapGrid, tile.GridIndices, out var reason))
            {
                _popup.PopupClient(reason, args.User, args.User);
                return;
            }
            var coordinates = GetCoordinates(args.TargetCoordinates);
            var baseTurf = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            if (HasBaseTurf(currentTileDefinition, baseTurf.ID))
            {
                PlaceAt(args.User, gridUid, mapGrid, location, currentTileDefinition.TileId);
                args.Handled = true;
                return;
            }
        }
        //Excuse moi,no grid creation for the spiders
    }

    public bool HasBaseTurf(ContentTileDefinition tileDef, string baseTurf)
    {
        return tileDef.BaseTurf == baseTurf;
    }

    private void PlaceAt(EntityUid user, EntityUid gridUid, MapGridComponent mapGrid, EntityCoordinates location,
        ushort tileId, float offset = 0)
    {
        _adminLogger.Add(LogType.Tile, LogImpact.Low, $"{ToPrettyString(user):actor} placed tile {_tileDefinitionManager[tileId].Name} at {ToPrettyString(gridUid)} {location}");

        var random = new System.Random((int)_timing.CurTick.Value);
        var variant = _tile.PickVariant((ContentTileDefinition)_tileDefinitionManager[tileId], random);
        _mapSystem.SetTile(gridUid, mapGrid, location.Offset(new Vector2(offset, offset)), new Tile(tileId, 0, variant));
    }

    public bool CanPlaceTile(EntityUid gridUid, MapGridComponent component, Vector2i gridIndices, [NotNullWhen(false)] out string? reason)
    {
        var ev = new FloorTileAttemptEvent(gridIndices);
        RaiseLocalEvent(gridUid, ref ev);

        if (ev.Cancelled)
        {
            reason = Loc.GetString("invalid-floor-placement");
            return false;
        }

        reason = null;
        return true;
    }
    /// <summary>
    /// Do a station announcement if all conditions are met
    /// </summary>
    private void DoStationAnnouncement(EntityUid uid, SpiderQueenComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            component.IsAnnouncedOnce ||
            !TryComp<TransformComponent>(uid, out var xform))
            return;

        var msg = Loc.GetString("spider-queen-warning",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, xform)))));
        _chat.DispatchGlobalAnnouncement(msg, playSound: false, colorOverride: Color.Red);
        _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
        component.IsAnnouncedOnce = true;
    }

    /// <summary>
    /// Converts hunger into blood points based on the <see cref="SpiderQueenComponent.HungerConvertCoefficient"/>
    /// </summary>
    private void ConvertHungerIntoBloodPoints(EntityUid uid, SpiderQueenComponent component, float amount, HungerComponent? hunger = null)
    {
        if (!Resolve(uid, ref hunger))
            return;

        var amountToMax = component.MaxBloodPoints - component.CurrentBloodPoints;
        if (amountToMax <= FixedPoint2.Zero)
            return;

        var value = amount * component.HungerConvertCoefficient;
        value = MathF.Min(value, (float)amountToMax);

        var hungerDecreaseValue = -(value / component.HungerConvertCoefficient);
        _hunger.ModifyHunger(uid, hungerDecreaseValue, hunger);
        ChangeBloodPointsAmount(uid, component, value);
    }

    private bool TryStartSpiderSpawnDoAfter(EntityUid spider,
        TimeSpan doAfter,
        EntityCoordinates coordinates,
        List<EntitySpawnEntry> prototypes,
        Vector2 offset,
        bool snapToGrid,
        FixedPoint2 cost)
    {
        var netCoordinates = GetNetCoordinates(coordinates);
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            spider,
            doAfter,
            new SpiderSpawnDoAfterEvent()
            {
                TargetCoordinates = netCoordinates,
                Prototypes = prototypes,
                Offset = offset,
                SnapToGrid = snapToGrid,
                Cost = cost,
            },
            null
        )
        {
            Broadcast = true,
            BreakOnDamage = false,
            BreakOnMove = true,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        var started = _doAfter.TryStartDoAfter(doAfterArgs);
        return started;
    }
}
