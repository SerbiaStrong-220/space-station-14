// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.Trap;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;

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
        SubscribeLocalEvent<CultYoggTrapComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CultYoggTrapComponent, TrapArmAttemptEvent>(OnTrapArmAttempt);
        SubscribeLocalEvent<ChangeCultYoggStageEvent>(OnStageChanged);
        SubscribeLocalEvent<CultYoggTrapComponent, TrapArmedEvent>(OnTrapArmed);
        SubscribeLocalEvent<CultYoggTrapComponent, TrapDefusedEvent>(OnTrapDefused);
        SubscribeLocalEvent<CultYoggTrapComponent, AttemptTriggerEvent>(OnAttemptTrigger, before:[ typeof(TriggerSystem) ]);
    }

    private void OnMapInit(Entity<CultYoggTrapComponent> ent, ref MapInitEvent args)
    {
        _stealth.SetEnabled(ent.Owner, false);
    }

    private void OnTrapArmAttempt(Entity<CultYoggTrapComponent> ent, ref TrapArmAttemptEvent args)
    {
        if (args.Cancelled || !args.User.HasValue)
            return;

        if (!TryComp<CultYoggComponent>(args.User, out var cultYoggComp))
        {
            args.Cancelled = true;
            return;
        }

        if (cultYoggComp.CurrentStage == CultYoggStage.Alarm)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-trap-component-alarm-stage"), args.User.Value, args.User.Value);
            args.Cancelled = true;
            return;
        }

        HashSet<EntityUid> trapYoggList = new();
        var query = AllEntityQuery<CultYoggTrapComponent, TrapComponent>();
        while (query.MoveNext(out var yoggTrap, out _, out var queryTrapComp))
        {
            if(queryTrapComp.State == TrapArmedState.Armed)
                trapYoggList.Add(yoggTrap);
        }

        if (trapYoggList.Count >= ent.Comp.TrapsLimit && ent.Comp.TrapsLimit > 0)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-trap-component-max-value"), args.User.Value, args.User.Value);
            args.Cancelled = true;
        }
    }

    private void OnStageChanged(ref ChangeCultYoggStageEvent args)
    {
        if (args.Stage != CultYoggStage.Alarm)
            return;

        var query = AllEntityQuery<CultYoggTrapComponent, TrapComponent>();
        while (query.MoveNext(out var yoggTrap, out _, out var queryTrapComp))
        {
            if(queryTrapComp.State == TrapArmedState.Armed)
                RemComp<StealthComponent>(yoggTrap);
        }
    }

    private void OnTrapArmed(Entity<CultYoggTrapComponent> ent, ref TrapArmedEvent args)
    {
        SetStealthTrap(ent, true);
    }

    private void OnTrapDefused(Entity<CultYoggTrapComponent> ent, ref TrapDefusedEvent args)
    {
        SetStealthTrap(ent, false);
    }

    private void OnAttemptTrigger(Entity<CultYoggTrapComponent> ent, ref AttemptTriggerEvent args)
    {
        args.Cancelled = TryComp<TrapComponent>(ent.Owner, out var trap) && trap.State == TrapArmedState.Unarmed;
    }

    public void SetStealthTrap(Entity<CultYoggTrapComponent> ent, bool isArmed)
    {
        if(!TryComp<StealthComponent>(ent.Owner, out var stealth))
            return;

        var visibility = isArmed ? ent.Comp.ArmedVisibility : ent.Comp.UnArmedVisibility;
        _stealth.SetEnabled(ent.Owner, isArmed, stealth);
        _stealth.SetVisibility(ent.Owner, visibility, stealth);
    }
}
