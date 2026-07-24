// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.SS220.Clothing.Components;
using Content.Shared.SS220.Clothing.Systems;
using Content.Shared.SS220.PoweredClothing;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.PhysicalParameters;

public abstract class SharedPoweredClothingSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!; 
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PoweredClothingComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<RelayedIntegratedClothingPowerSourceComponent, MapInitEvent>(OnRelayCompMapInit, after: [typeof(IntegratedClothingSystem)]);

        SubscribeLocalEvent<PoweredClothingComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
        SubscribeLocalEvent<PoweredClothingComponent, ItemToggleDeactivateAttemptEvent>(TryTurnOff);
        SubscribeLocalEvent<PoweredClothingComponent, ItemToggledEvent>(OnActivated);

        SubscribeLocalEvent<PoweredClothingComponent, ClothingGotUnequippedEvent>(OnGotUnEquipped);
    }

    public void TryTurnOn(Entity<PoweredClothingComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User == null)
            return;

        if (!_actionBlocker.CanComplexInteract(args.User.Value))
        {
            args.Cancelled = true;
            return;
        }

        if (!TryComp<ComponentRequiringPoweredClothingComponent>(entity.Owner, out var compReqiringComp) ||
            !_containerSystem.TryGetContainingContainer(entity.Owner, out var userContainer) ||
            !_whitelist.IsWhitelistPassOrNull(compReqiringComp.Whitelist, userContainer.Owner))
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
        if (args.Activated)
        {
            EnsureComp<ActivePoweredClothingComponent>(entity.Owner);
            return;
        }

        RemComp<ActivePoweredClothingComponent>(entity.Owner);
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

    private void OnGotUnEquipped(Entity<PoweredClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _itemToggle.TryDeactivate(ent.Owner);
    }
}
