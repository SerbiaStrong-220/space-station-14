// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
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
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderQueenComponent, AfterCocooningEvent>(OnAfterCocooning);
        SubscribeLocalEvent<SpiderQueenComponent, SpiderTargetSpawnEvent>(OnTargetSpawn);
        SubscribeLocalEvent<SpiderQueenComponent, SpiderSpawnDoAfterEvent>(OnSpawnDoAfter);
        SubscribeLocalEvent<SpiderQueenComponent, SpiderNearbySpawnEvent>(OnNearbySpawn);
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

    private void OnTargetSpawn(Entity<SpiderQueenComponent> entity, ref SpiderTargetSpawnEvent args)
    {
        var spider = entity.Owner;
        if (args.Handled ||
            spider != args.Performer ||
            !CheckEnoughBloodPoints(spider, args.Cost, entity.Comp))
            return;

        if (TryStartSpiderSpawnDoAfter(spider, args.DoAfter, args.Target, args.Prototypes, args.Offset, args.Cost))
        {
            args.Handled = true;
        }
        else
        {
            Log.Error($"Failed to start DoAfter by {spider}");
            return;
        }
    }

    private void OnNearbySpawn(Entity<SpiderQueenComponent> entity, ref SpiderNearbySpawnEvent args)
    {
        var spider = entity.Owner;
        if (args.Handled ||
            spider != args.Performer ||
            !TryComp<TransformComponent>(entity.Owner, out var transform) ||
            !CheckEnoughBloodPoints(spider, args.Cost, entity.Comp))
            return;

        if (TryStartSpiderSpawnDoAfter(spider, args.DoAfter, transform.Coordinates, args.Prototypes, args.Offset, args.Cost))
        {
            args.Handled = true;
        }
        else
        {
            Log.Error($"Failed to start DoAfter by {spider}");
            return;
        }
    }

    private void OnSpawnDoAfter(Entity<SpiderQueenComponent> entity, ref SpiderSpawnDoAfterEvent args)
    {
        if (args.Cancelled ||
            !CheckEnoughBloodPoints(entity, args.Cost, entity.Comp))
            return;

        entity.Comp.CurrentBloodPoints -= args.Cost;
        Dirty(entity);
        UpdateAlert(entity);

        var getProtos = EntitySpawnCollection.GetSpawns(args.Prototypes, _random);
        var targetMapCords = GetCoordinates(args.TargetCoordinates);
        foreach (var proto in getProtos)
        {
            Spawn(proto, targetMapCords.SnapToGrid(EntityManager, _mapManager));
            targetMapCords = targetMapCords.Offset(args.Offset);
        }
    }

    private void OnAfterCocooning(Entity<SpiderQueenComponent> entity, ref AfterCocooningEvent args)
    {
        if (args.Cancelled || args.Target is not EntityUid target)
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
        component.CurrentBloodPoints += value;
        Dirty(uid, component);
        UpdateAlert((uid, component));
    }

    private bool TryStartSpiderSpawnDoAfter(EntityUid spider,
        TimeSpan doAfter,
        EntityCoordinates coordinates,
        List<EntitySpawnEntry> prototypes,
        Vector2 offset,
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
                Cost = cost,
            },
            spider
        )
        {
            Broadcast = false,
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
