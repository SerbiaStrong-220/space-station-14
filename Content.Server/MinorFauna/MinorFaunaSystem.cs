using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Content.Shared.SS220.MinorFauna.Actions;
using Content.Shared.SS220.MinorFauna.Events;
using Content.Shared.SS220.MinorFauna.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.SS220.MinorFauna;

public abstract class SharedMinorFaunaSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CocoonerComponent, AfterEntityCocooningEvent>(OnAfterEntityCocooningEvent);
    }

    private void OnAfterEntityCocooningEvent(Entity<CocoonerComponent> entity, ref AfterEntityCocooningEvent args)
    {
        if (args.Cancelled || args.Target is not EntityUid target)
            return;

        if (!TryComp<TransformComponent>(target, out var transform) || !_mobState.IsDead(target))
            return;
        var targetCords = _transform.GetMoverCoordinates(target, transform);
        var cocoonPrototypeID = _random.Pick(entity.Comp.CocoonPrototypes);
        var cocoonUid = Spawn(cocoonPrototypeID, targetCords);

        if (!TryComp<EntityCocoonComponent>(cocoonUid, out var cocoon) ||
            !_container.TryGetContainer(cocoonUid, cocoon.CocoonContainerId, out var container))
        {
            Log.Error($"{cocoonUid} doesn't have required components to cocooning target");
            return;
        }

        _container.Insert(target, container);
    }

}
