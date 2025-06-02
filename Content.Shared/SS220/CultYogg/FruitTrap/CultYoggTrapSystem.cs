// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.Trap;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Shared.SS220.CultYogg.FruitTrap;

/// <summary>
/// Modified <see cref="TrapSystem"/> for cult traps. All modifications add additional conditions.
/// </summary>
public sealed class CultYoggTrapSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    private readonly HashSet<EntityUid> _trapYoggList = new();


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CultYoggTrapComponent, MapInitEvent>(OnStartUp);
        SubscribeLocalEvent<CultYoggTrapComponent, DoAfterAttemptEvent<TrapInteractionDoAfterEvent>>(CheckInteract);
        SubscribeLocalEvent<ChangeCultYoggStageEvent>(OnAlarmStage);
        SubscribeLocalEvent<CultYoggTrapComponent, TrapToggledEvent>(OnChangedArmed);
    }

    private void OnStartUp(Entity<CultYoggTrapComponent> ent, ref MapInitEvent args)
    {
        _stealth.SetEnabled(ent.Owner, false);
    }

    private void CheckInteract(Entity<CultYoggTrapComponent> ent, ref DoAfterAttemptEvent<TrapInteractionDoAfterEvent> args)
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

        if (cultYoggComp.CurrentStage == CultYoggStage.Alarm)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-trap-component-alarm-stage"), args.DoAfter.Args.User, args.DoAfter.Args.User);
            args.Cancel();
        }
        else if (_trapYoggList.Count >= ent.Comp.TrapsLimit
            && ent.Comp.TrapsLimit > 0)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-trap-component-max-value"), args.DoAfter.Args.User, args.DoAfter.Args.User);
            args.Cancel();
        }
    }

    private void OnAlarmStage(ref ChangeCultYoggStageEvent args)
    {
        if (args.Stage != CultYoggStage.Alarm)
            return;

        foreach (var currentTrap in _trapYoggList) //only for already installed
        {
            RemComp<StealthComponent>(currentTrap);
        }
    }

    private void OnChangedArmed(Entity<CultYoggTrapComponent> ent, ref TrapToggledEvent args)
    {
        var visibility = args.IsArmed ? ent.Comp.ArmedVisibility : ent.Comp.UnArmedVisibility;
        _stealth.SetEnabled(ent.Owner, args.IsArmed);
        _stealth.SetVisibility(ent.Owner, visibility);

        if (args.IsArmed)
            _trapYoggList.Add(ent.Owner);
        else
            _trapYoggList.Remove(ent.Owner);
    }
}
