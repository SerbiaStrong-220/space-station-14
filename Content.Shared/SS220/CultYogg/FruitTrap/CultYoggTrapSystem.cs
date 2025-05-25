// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.Trap;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Shared.SS220.CultYogg.FruitTrap;

/// <summary>
/// This handlesModified <see cref="TrapSystem"/> for cult traps. All modifications add additional conditions.
/// </summary>
public sealed class CultYoggTrapSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    private readonly HashSet<EntityUid> _trapYoggList = new();


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CultYoggTrapComponent, DoAfterAttemptEvent<InteractionTrapDoAfterEvent>>(CheckInteract);
        SubscribeLocalEvent<CultYoggTrapComponent, ChangeCultYoggStageEvent>(OnChangeStage);
        SubscribeLocalEvent<CultYoggTrapComponent, TrapChangedArmedEvent>(OnChangedArmed);
    }

    private void CheckInteract(Entity<CultYoggTrapComponent> ent, ref DoAfterAttemptEvent<InteractionTrapDoAfterEvent> args)
    {
        if (!TryComp<TrapComponent>(ent.Owner, out var trapComp))
            return;

        if (trapComp.IsArmed)
            return; //further logic is only needed to set the trap


        if (!TryComp<CultYoggComponent>(args.DoAfter.Args.User, out var cultYoggComp))
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-trap-component-no-cultYogg"), args.DoAfter.Args.User, args.DoAfter.Args.User);
            args.Cancel();
            return;
        }

        _trapYoggList.Clear();
        var query = AllEntityQuery<CultYoggTrapComponent>();

        while (query.MoveNext(out var fruitTrap, out _))
        {
            if(!TryComp<TrapComponent>(fruitTrap, out var trapCompForEchFruit))
                continue;

            if(trapCompForEchFruit.IsArmed)
                _trapYoggList.Add(fruitTrap);
        }

        if (cultYoggComp.CurrentStage == CultYoggStage.Alarm)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-trap-component-alarm-stage"), args.DoAfter.Args.User, args.DoAfter.Args.User);
            args.Cancel();
        }
        else if (_trapYoggList.Count >= ent.Comp.MaxTrap
            && ent.Comp.MaxTrap != 0)
        {

            _popup.PopupClient(Loc.GetString("cult-yogg-trap-component-max-value"), args.DoAfter.Args.User, args.DoAfter.Args.User);
            args.Cancel();
        }
    }

    private void OnChangeStage(Entity<CultYoggTrapComponent> ent, ref ChangeCultYoggStageEvent args)
    {
        if (args.Stage != CultYoggStage.Alarm)
            return;

        foreach (var currentTrap in _trapYoggList) //only for already installed
        {
            RemComp<StealthComponent>(currentTrap);
        }
    }

    private void OnChangedArmed(Entity<CultYoggTrapComponent> ent, ref TrapChangedArmedEvent args)
    {
        var visibility = args.NewIsArmed ? ent.Comp.VisibilityOnArmed : ent.Comp.VisibilityOnUnArmed;
        _stealth.SetVisibility(ent.Owner, visibility);
    }
}
