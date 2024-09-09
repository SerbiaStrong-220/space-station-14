// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Popups;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.SpiderQueen;
using Content.Shared.SS220.SpiderQueen.Components;
using Content.Shared.SS220.SpiderQueen.Systems;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.SS220.SpiderQueen;

public sealed partial class SpiderQueenSystem : SharedSpiderQueenSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderWorldSpawnEvent>(OnWorldSpawn);

        SubscribeLocalEvent<SpiderQueenComponent, AfterCocooningEvent>(OnAfterCocooning);
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
        var cocoonUid = Spawn(entity.Comp.CocoonProto, targetCords.SnapToGrid(EntityManager, _mapManager));

        if (!TryComp<SpiderCocoonComponent>(cocoonUid, out var spiderCocoon) ||
            !_container.TryGetContainer(cocoonUid, spiderCocoon.CocoonContainerId, out var container))
        {
            Log.Error($"{cocoonUid} doesn't have required components to cocooning target");
            return;
        }

        _container.Insert(target, container);
    }
}
