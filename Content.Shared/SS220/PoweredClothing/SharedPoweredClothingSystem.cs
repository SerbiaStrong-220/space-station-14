// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.SS220.Clothing.Components;
using Content.Shared.SS220.Clothing.Systems;
using Content.Shared.SS220.PoweredClothing;

namespace Content.Shared.SS220.PhysicalParameters;

public abstract class SharedPoweredClothingSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PoweredClothingComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<RelayedIntegratedClothingPowerSourceComponent, MapInitEvent>(OnRelayCompMapInit, after: [typeof(IntegratedClothingSystem)]);
        SubscribeLocalEvent<PoweredClothingComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
        SubscribeLocalEvent<PoweredClothingComponent, ItemToggleDeactivateAttemptEvent>(TryTurnOff);
        SubscribeLocalEvent<PoweredClothingComponent, ItemToggledEvent>(OnActivated);
    }

    public void TryTurnOn(Entity<PoweredClothingComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User != null && !_actionBlocker.CanComplexInteract(args.User.Value))
        {
            args.Cancelled = true;
            return;
        }
    }

    public void TryTurnOff(Entity<PoweredClothingComponent> entity, ref ItemToggleDeactivateAttemptEvent args)
    {
        if (args.User != null && !_actionBlocker.CanComplexInteract(args.User.Value))
        {
            args.Cancelled = true;
            return;
        }
    }

    public void OnCompInit(Entity<PoweredClothingComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.SelfPowered)
            ent.Comp.PowerSource = ent.Owner;

        Dirty(ent);
    }

    public void OnActivated(Entity<PoweredClothingComponent> entity, ref ItemToggledEvent args)
    {
        EnsureComp<ActivePoweredClothingComponent>(entity.Owner);
    }

    public void OnRelayCompMapInit(Entity<RelayedIntegratedClothingPowerSourceComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<PoweredClothingComponent>(ent.Owner, out var powerComp) || !TryComp<IntegratedClothingComponent>(ent.Owner, out var integratedComp))
            return;

        if (!integratedComp.ClothingUids.TryGetValue(ent.Comp.Slot, out var powerProviderUid) || powerProviderUid is not { Valid: true } containedEntValidated)
            return;

        powerComp.PowerSource = containedEntValidated;

        Dirty(ent.Owner, powerComp);
    }
}
