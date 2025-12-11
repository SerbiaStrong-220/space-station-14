// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Pinpointer;
using Content.Server.SS220.SpiderQueen.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.SS220.SpiderQueen;
using Content.Shared.SS220.SpiderQueen.Components;
using Content.Shared.SS220.SpiderQueen.Systems;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
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
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;

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

        var mapCoords = _transform.ToMapCoordinates(args.Target);
        if (!_interaction.InRangeUnobstructed(performer, mapCoords))
            return;

        if (GetNearestGrid(mapCoords) == null)
            return;

        var netCoordinates = GetNetCoordinates(args.Target);
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            args.DoAfter,
            new SpiderTileSpawnDoAfterEvent()
            {
                Prototype = args.Prototype,
                InSpacePrototype = args.InSpacePrototype,
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
        var user = args.User;
        if (args.Cancelled
            || args.Handled
            || !CheckEnoughBloodPoints(user, args.Cost))
            return;

        var targetCoords = GetCoordinates(args.TargetCoordinates);
        var mapCoords = _transform.ToMapCoordinates(targetCoords);
        if (!_interaction.InRangeUnobstructed(user, mapCoords))
            return;

        var grid = GetNearestGrid(mapCoords);
        if (grid == null)
            return;

        var gridCoords = _transform.ToCoordinates(grid.Value.Owner, mapCoords);
        var curTile = _mapSystem.GetTileRef(grid.Value, gridCoords);

        var tileProto = args.InSpacePrototype is { } spaceProto && _turf.IsSpace(curTile)
            ? spaceProto
            : args.Prototype;

        var placedTile = _tileDefinitionManager[tileProto];
        var variant = _tile.PickVariant((ContentTileDefinition)placedTile);

        _mapSystem.SetTile(grid.Value, curTile.GridIndices, new Tile(placedTile.TileId, variant: variant));

        if (TryComp<SpiderQueenComponent>(args.User, out var spiderQueen))
            ChangeBloodPointsAmount(args.User, spiderQueen, -args.Cost);

        args.Handled = true;
    }

    private Entity<MapGridComponent>? GetNearestGrid(MapCoordinates mapCoords, float range = 1f)
    {
        var circle = new PhysShapeCircle(range, mapCoords.Position);
        var grids = new List<Entity<MapGridComponent>>();
        _mapManager.FindGridsIntersecting(mapCoords.MapId, circle, Robust.Shared.Physics.Transform.Empty, ref grids, includeMap: false);

        Entity<MapGridComponent>? result = null;
        var distance = float.PositiveInfinity;
        var circleBox = circle.CalcLocalBounds();
        foreach (var grid in grids)
        {
            var gridXform = Transform(grid);
            var worldMatrix = _transform.GetWorldMatrix(gridXform);
            var gridIntersect = circleBox.Intersect(worldMatrix.TransformBox(grid.Comp.LocalAABB));
            var gridDist = (gridIntersect.Center - mapCoords.Position).LengthSquared();

            if (gridDist >= distance)
                continue;

            result = grid;
            distance = gridDist;
        }

        return result;
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
