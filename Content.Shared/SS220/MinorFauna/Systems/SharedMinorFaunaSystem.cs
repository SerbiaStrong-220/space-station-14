using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.MinorFauna.Actions;
using Content.Shared.SS220.MinorFauna.Events;
using Content.Shared.SS220.MinorFauna.Components;
using Content.Shared.Humanoid;


namespace Content.Shared.SS220.MinorFauna.Systems;

public sealed class SharedMinorFaunaSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CocoonerComponent, ActionEntityCocooningEvent>(OnCocooningAction);
    }

    private void OnCocooningAction(Entity<CocoonerComponent> entity, ref ActionEntityCocooningEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        var target = args.Target;

        if (!_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("cocooning-target-not-dead"), performer, performer);
            return;
        }

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            args.CocooningTime,
            new AfterEntityCocooningEvent(),
            performer,
            target
        );

        var started = _doAfter.TryStartDoAfter(doAfterArgs);
        if (started)
        {
            args.Handled = true;
        }
        else
        {
            Log.Error($"Failed to start DoAfter by {performer}");
            return;
        }
    }

}
