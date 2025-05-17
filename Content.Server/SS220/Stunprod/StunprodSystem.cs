using Content.Server.Item;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.SS220.Stunprod;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.SS220.Stunprod;

public sealed class StunprodSystem : SharedStunprodSystem
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly ItemSystem _item = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunprodComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<StunprodComponent, ItemToggledEvent>(OnItemToggled);
        SubscribeLocalEvent<StunprodComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<StunprodComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<StunprodComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<StunprodComponent, PowerCellSlotEmptyEvent>(OnRemoveBattery);
    }

    private void OnExamined(Entity<StunprodComponent> ent, ref ExaminedEvent args)
    {
        var onMsg = _itemToggle.IsActivated(ent.Owner)
            ? Loc.GetString("comp-stunbaton-examined-on")
            : Loc.GetString("comp-stunbaton-examined-off");

        args.PushMarkup(onMsg);

        if (FindBattery(ent) == null)
            return;

        var count = (int) (FindBattery(ent)!.CurrentCharge / ent.Comp.EnergyPerUse);
        args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
    }

    private void OnItemToggled(Entity<StunprodComponent> entity, ref ItemToggledEvent args)
    {
        _item.SetHeldPrefix(entity.Owner, args.Activated ? "on" : "off");
    }

    private void OnActivateAttempt(Entity<StunprodComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (FindBattery(ent) != null && !(FindBattery(ent)!.CurrentCharge < ent.Comp.EnergyPerUse))
            return;

        if (args.User != null)
            _popup.PopupEntity(Loc.GetString("stunbaton-component-low-charge"), args.User.Value, args.User.Value);

        args.Cancelled = true;
    }

    private void OnMeleeHit(Entity<StunprodComponent> ent, ref MeleeHitEvent args)
    {
        ChangeEnergy(ent);
    }

    private void OnThrowHit(Entity<StunprodComponent> ent, ref ThrowDoHitEvent args)
    {
        ChangeEnergy(ent);
    }

    private void OnRemoveBattery(Entity<StunprodComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        _itemToggle.TryDeactivate(ent.Owner);
    }

    private BatteryComponent? FindBattery(Entity<StunprodComponent> ent)
    {
        if (!TryComp<PowerCellSlotComponent>(ent.Owner, out var cellSlot)
            || !_itemSlots.TryGetSlot(ent.Owner, cellSlot.CellSlotId, out var itemSlot)
            || !TryComp<BatteryComponent>(itemSlot.Item, out var batteryComponent))
            return null;

        return batteryComponent;
    }

    private void ChangeEnergy(Entity<StunprodComponent> ent)
    {
        if (FindBattery(ent) == null)
            return;

        if (!_itemToggle.IsActivated(ent.Owner))
            return;

        if(!_battery.TryUseCharge(FindBattery(ent)!.Owner, ent.Comp.EnergyPerUse))
            _itemToggle.TryDeactivate(ent.Owner);
    }
}
