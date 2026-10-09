using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.SS220.Hookah.Components;
using Content.Shared.SS220.HookahElectric;
using Content.Shared.SS220.HookahElectric.Components;
using Content.Shared.Verbs;

namespace Content.Server.SS220.Hookah;

public sealed partial class HookahSystem
{
    private static readonly LocId HookahHosesFull = "hookah-electric-hoses-full";
    private static readonly LocId HookahNoBattery = "hookah-electric-no-battery";
    private static readonly LocId HookahLowBattery = "hookah-electric-low-battery";
    private static readonly LocId HookahTurnedOn = "hookah-electric-turned-on";
    private static readonly LocId HookahTurnedOff = "hookah-electric-turned-off";
    private static readonly LocId HookahBatteryOut = "hookah-electric-battery-out";
    private static readonly LocId HookahElectricNotOn = "hookah-electric-not-on";
    private static readonly LocId HookahVerbTurnOn = "hookah-electric-verb-turn-on";
    private static readonly LocId HookahVerbTurnOff = "hookah-electric-verb-turn-off";
    private static readonly LocId HookahVerbEjectCell = "hookah-electric-verb-eject-cell";

    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private void InitializeElectric()
    {
        SubscribeLocalEvent<HookahElectricComponent, GetVerbsEvent<AlternativeVerb>>(OnGetElectricVerbs);
        SubscribeLocalEvent<HookahElectricComponent, GetVerbsEvent<Verb>>(OnGetElectricEjectVerb);
        SubscribeLocalEvent<HookahElectricComponent, PowerCellSlotEmptyEvent>(OnPowerCellEmpty);
        SubscribeLocalEvent<HookahElectricComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void TryTakeElectricHose(
        Entity<HookahComponent> ent,
        HookahElectricComponent electric,
        ref InteractHandEvent args)
    {
        HookahElectricHoseSide? side = null;
        if (electric.LeftHose == null || TerminatingOrDeleted(electric.LeftHose.Value))
            side = HookahElectricHoseSide.Left;
        else if (electric.RightHose == null || TerminatingOrDeleted(electric.RightHose.Value))
            side = HookahElectricHoseSide.Right;

        if (side == null)
        {
            _popup.PopupEntity(Loc.GetString(HookahHosesFull), ent, args.User);
            args.Handled = true;
            return;
        }

        var offset = side == HookahElectricHoseSide.Left
            ? electric.LeftHoseOffset
            : electric.RightHoseOffset;

        var hose = SpawnHookahHose(ent, args.User, offset, side);
        if (side == HookahElectricHoseSide.Left)
            electric.LeftHose = hose;
        else
            electric.RightHose = hose;

        Dirty(ent.Owner, electric);
        UpdateElectricAppearance(ent, electric);
        args.Handled = true;
    }

    private void OnGetElectricVerbs(Entity<HookahElectricComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !TryComp(ent.Owner, out HookahComponent? hookah))
            return;

        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString(hookah.IsLit ? HookahVerbTurnOff : HookahVerbTurnOn),
            Act = () => TryToggleElectric((ent.Owner, hookah), ent.Comp, user),
            Priority = 1,
        };

        args.Verbs.Add(verb);
    }

    private void OnGetElectricEjectVerb(Entity<HookahElectricComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, "cell_slot", out var slot) || slot.Item == null)
            return;

        var user = args.User;
        var verb = new Verb
        {
            Text = Loc.GetString(HookahVerbEjectCell),
            Category = VerbCategory.Eject,
            Act = () => _itemSlots.TryEjectToHands(ent.Owner, slot, user, excludeUserAudio: true),
        };

        args.Verbs.Add(verb);
    }

    private void TryToggleElectric(Entity<HookahComponent> ent, HookahElectricComponent electric, EntityUid user)
    {
        if (ent.Comp.IsLit)
        {
            SetElectricPowered(ent, electric, false);
            _popup.PopupEntity(Loc.GetString(HookahTurnedOff), ent, user);
            return;
        }

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out _))
        {
            _popup.PopupEntity(Loc.GetString(HookahNoBattery), ent, user);
            return;
        }

        if (!_powerCell.HasDrawCharge(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString(HookahLowBattery), ent, user);
            return;
        }

        SetElectricPowered(ent, electric, true);
        _popup.PopupEntity(Loc.GetString(HookahTurnedOn), ent, user);
    }

    private void SetElectricPowered(Entity<HookahComponent> ent, HookahElectricComponent electric, bool on)
    {
        if (ent.Comp.IsLit == on)
            return;

        ent.Comp.IsLit = on;
        Dirty(ent);

        _powerCell.SetDrawEnabled(ent.Owner, on);
        UpdateElectricAppearance(ent, electric);
        _audio.PlayPvs(on ? electric.ToggleOnSound : electric.ToggleOffSound, ent);
    }

    private void OnPowerCellEmpty(Entity<HookahElectricComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (!TryComp(ent.Owner, out HookahComponent? hookah) || !hookah.IsLit)
            return;

        SetElectricPowered((ent.Owner, hookah), ent.Comp, false);
        BatteryOutPopup(ent.Comp);
    }

    private void OnPowerCellChanged(Entity<HookahElectricComponent> ent, ref PowerCellChangedEvent args)
    {
        if (!args.Ejected || !TryComp(ent.Owner, out HookahComponent? hookah) || !hookah.IsLit)
            return;

        SetElectricPowered((ent.Owner, hookah), ent.Comp, false);
        BatteryOutPopup(ent.Comp);
    }

    private void BatteryOutPopup(HookahElectricComponent electric)
    {
        foreach (var hose in new[] { electric.LeftHose, electric.RightHose })
        {
            if (hose is not { } h ||
                TerminatingOrDeleted(h) ||
                !_container.TryGetContainingContainer(h, out var container) ||
                !HasComp<HandsComponent>(container.Owner))
                continue;

            _popup.PopupEntity(Loc.GetString(HookahBatteryOut), container.Owner, container.Owner);
        }
    }

    private void UpdateElectricAppearance(Entity<HookahComponent> ent, HookahElectricComponent electric)
    {
        _appearance.SetData(ent, HookahElectricVisuals.Enabled, ent.Comp.IsLit);
        _appearance.SetData(ent, HookahElectricVisuals.LeftHose,
            electric.LeftHose == null || TerminatingOrDeleted(electric.LeftHose.Value));
        _appearance.SetData(ent, HookahElectricVisuals.RightHose,
            electric.RightHose == null || TerminatingOrDeleted(electric.RightHose.Value));
    }
}
