using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.SpiderQueen.Components;
using Content.Shared.SS220.SpiderQueen;

namespace Content.Shared.SS220.MinorFauna.Systems;

public abstract class SharedMinorFaunaSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MinorFaunaComponent, ActionSpiderCocooningEvent>(OnCocooningAction);
    }

    private void OnCocooningAction(Entity<MinorFaunaComponent> entity, ref ActionSpiderCocooningEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        var target = args.Target;

        foreach (var entityInRange in _entityLookup.GetEntitiesInRange(target, entity.Comp.CocoonsMinDistance))
        {
            if (!HasComp<MinorFaunaComponent>(entityInRange))
                continue;

            _popup.PopupEntity(Loc.GetString("cocooning-too-close"), performer, performer);
            return;
        }

        if (!_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("cocooning-target-not-dead"), performer, performer);
            return;
        }

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            args.CocooningTime,
            new AfterSpiderCocooningEvent(),
            performer,
            target
        );
    }
    AfterSpiderCocooningEvent(Entity<MinorFaunaComponent> entity, ref AfterSpiderCocooningEvent args)
    {

    }

}
