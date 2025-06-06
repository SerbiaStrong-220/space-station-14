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

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CultYoggTrapComponent, MapInitEvent>(OnStartUp);
        SubscribeLocalEvent<CultYoggTrapComponent, DoAfterAttemptEvent<TrapSetDoAfterEvent>>(OnTrapSetDoAfter);
        SubscribeLocalEvent<ChangeCultYoggStageEvent>(OnStageChanged);
        SubscribeLocalEvent<CultYoggTrapComponent, TrapToggledEvent>(OnChangedArmed);
    }

    private void OnStartUp(Entity<CultYoggTrapComponent> ent, ref MapInitEvent args)
    {
        _stealth.SetEnabled(ent.Owner, false);
    }

    private void OnTrapSetDoAfter(Entity<CultYoggTrapComponent> ent, ref DoAfterAttemptEvent<TrapSetDoAfterEvent> args)
    {
        if (!HasComp<TrapComponent>(ent.Owner))
            return;

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
            return;
        }

        HashSet<EntityUid> trapYoggList = new();
        var query = AllEntityQuery<CultYoggTrapComponent, TrapComponent>();

        while (query.MoveNext(out var yoggTrap, out _, out var queryTrapComp))
        {
            if(queryTrapComp.IsArmed)
                trapYoggList.Add(yoggTrap);
        }

        if (trapYoggList.Count >= ent.Comp.TrapsLimit
            && ent.Comp.TrapsLimit > 0)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-trap-component-max-value"), args.DoAfter.Args.User, args.DoAfter.Args.User);
            args.Cancel();
        }
    }

    private void OnStageChanged(ref ChangeCultYoggStageEvent args)
    {
        if (args.Stage != CultYoggStage.Alarm)
            return;

        var query = AllEntityQuery<CultYoggTrapComponent, TrapComponent>();

        while (query.MoveNext(out var yoggTrap, out _, out var queryTrapComp))
        {
            if(queryTrapComp.IsArmed)
                RemComp<StealthComponent>(yoggTrap);
        }
    }

    private void OnChangedArmed(Entity<CultYoggTrapComponent> ent, ref TrapToggledEvent args)
    {
        if(!TryComp<StealthComponent>(ent.Owner, out var stealth))
            return;

        var visibility = args.IsArmed ? ent.Comp.ArmedVisibility : ent.Comp.UnArmedVisibility;
        _stealth.SetEnabled(ent.Owner, args.IsArmed, stealth);
        _stealth.SetVisibility(ent.Owner, visibility, stealth);
    }
}
