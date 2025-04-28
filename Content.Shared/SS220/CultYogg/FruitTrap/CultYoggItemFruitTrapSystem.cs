using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.Trap;

namespace Content.Shared.SS220.CultYogg.FruitTrap;

/// <summary>
/// This handles...
/// </summary>
public sealed class CultYoggItemFruitTrapSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private List<EntityUid> _trapYoggList = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CultYoggItemFruitTrapComponent, DoAfterAttemptEvent<SetTrapDoAfterEvent>>(OnAfterInteract);
        SubscribeLocalEvent<CultYoggItemFruitTrapComponent, SetTrapDoAfterEvent>(OnSetTrap);
    }

    private void OnAfterInteract(Entity<CultYoggItemFruitTrapComponent> ent, ref DoAfterAttemptEvent<SetTrapDoAfterEvent> args)
    {
        if(!TryComp<CultYoggComponent>(args.DoAfter.Args.User, out var cultYoggComp))
            return;

        if(cultYoggComp.CurrentStage == CultYoggStage.Alarm)
        {
            _popup.PopupEntity(Loc.GetString("trap-component-max-value"), args.DoAfter.Args.User, args.DoAfter.Args.User);
            args.Cancel();
        }

        if (_trapYoggList.Count > ent.Comp.MaxTrap || ent.Comp.MaxTrap != 0)
        {
            _popup.PopupEntity(Loc.GetString("trap-component-max-value"), args.DoAfter.Args.User, args.DoAfter.Args.User);
            args.Cancel();
        }
    }

    private void OnSetTrap(Entity<CultYoggItemFruitTrapComponent> ent, ref SetTrapDoAfterEvent args)
    {
        _trapYoggList.Add(args.TrapUid);
    }


}
