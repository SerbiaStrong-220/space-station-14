// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
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

namespace Content.Server.SS220.SpiderQueen;

public sealed partial class SpiderQueenSystem : SharedSpiderQueenSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderQueenComponent, AfterCocooningEvent>(OnAfterCocooning);
        SubscribeLocalEvent<SpiderCocoonComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<SpiderWorldSpawnEvent>(OnWorldSpawn);
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

            var newValue = comp.CurrentMana + comp.PassiveGeneration + comp.CocoonsManaBonus;
            comp.CurrentMana = newValue > comp.MaxMana
                ? comp.MaxMana
                : newValue;

            Dirty(uid, comp);
        }
    }

    private void OnWorldSpawn(SpiderWorldSpawnEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        if (args.Cost > FixedPoint2.Zero)
        {
            if (!TryComp<SpiderQueenComponent>(args.Performer, out var component) ||
                component is null)
                return;

            if (component.CurrentMana < args.Cost)
            {
                _popup.PopupEntity(Loc.GetString("spider-queen-not-enough-mana"), performer, performer);
                return;
            }

            component.CurrentMana -= args.Cost;
            Dirty(performer, component);
        }

        var getProtos = EntitySpawnCollection.GetSpawns(args.Prototypes, _random);
        var targetMapCords = args.Target;
        foreach (var proto in getProtos)
        {
            Spawn(proto, targetMapCords.SnapToGrid(EntityManager, _mapManager));
            targetMapCords = targetMapCords.Offset(args.Offset);
        }
        args.Handled = true;
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
        Dirty(entity.Owner, entity.Comp);

        spiderCocoon.CocoonOwner = entity.Owner;
        Dirty(cocoonUid, spiderCocoon);

        if (entity.Comp.CocoonsCountToAnnouncement is { } value &&
            entity.Comp.CocoonsList.Count >= value)
        {
            DoStationAnnouncement(entity);
        }

        UpdateCocoonsBonus(entity.Owner);
    }

    private void OnShutdown(Entity<SpiderCocoonComponent> entity, ref ComponentShutdown args)
    {
        var (uid, comp) = entity;
        if (comp.CocoonOwner is null ||
            !TryComp<SpiderQueenComponent>(comp.CocoonOwner, out var queenComponent))
            return;

        queenComponent.CocoonsList.Remove(uid);
        UpdateCocoonsBonus(comp.CocoonOwner.Value);
    }

    /// <summary>
    /// Updates the bonus that cocoons give
    /// </summary>
    private void UpdateCocoonsBonus(EntityUid spider, SpiderQueenComponent? component = null)
    {
        if (!Resolve(spider, ref component))
            return;

        var newBonus = FixedPoint2.Zero;
        var i = 0;
        foreach (var cocoon in component.CocoonsList)
        {
            if (!TryComp<SpiderCocoonComponent>(cocoon, out var spiderCocoon) ||
                !_container.TryGetContainer(cocoon, spiderCocoon.CocoonContainerId, out var container) ||
                container.Count <= 0)
                continue;

            newBonus += spiderCocoon.ManaGenerationBonus * Math.Pow(component.CocoonsBonusCoefficient.Double(), i);
            i++;
        }

        component.CocoonsManaBonus = newBonus;
        Dirty(spider, component);
    }

    /// <summary>
    /// Do a station announcement if all conditions are met
    /// </summary>
    private void DoStationAnnouncement(EntityUid uid, SpiderQueenComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            component.IsAnnounced ||
            !TryComp<TransformComponent>(uid, out var xform))
            return;

        var msg = Loc.GetString("spider-queen-warning",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, xform)))));
        _chat.DispatchGlobalAnnouncement(msg, playSound: false, colorOverride: Color.Red);
        _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
        component.IsAnnounced = true;
    }
}
