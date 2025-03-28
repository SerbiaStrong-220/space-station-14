// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Fluids;
using Content.Shared.Inventory;
using Content.Shared.SS220.Spray.Components;
using Content.Shared.SS220.Spray.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Spray.Systems;

public abstract class SharedSpraySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClothingSlotSolutionProviderComponent, SprayAttemptEvent>(OnClothingTakeSolution);
        SubscribeLocalEvent<ClothingSlotSolutionProviderComponent, GetSolutionCountEvent>(OnClothingSolutionCount);
        SubscribeLocalEvent<SolutionProviderComponent, ComponentInit>(OnComponentInit);
    }

    protected virtual void OnComponentInit(EntityUid uid, SolutionProviderComponent tank, ComponentInit args)
    {

        ItemSlotsSystem.AddItemSlot(uid, SolutionProviderComponent.NozzleSlot, tank.TankSlot);

        UpdateTankAppearance(uid, tank);
    }

    private void OnClothingTakeSolution(Entity<ClothingSlotSolutionProviderComponent> ent, ref SprayAttemptEvent args)
    {
        if (!TryGetClothingSlotEntity(ent.Owner, ent.Comp, out var tank))
            return;
        if (!_solutionContainerSystem.TryGetSolution(tank.Value, ent.Comp.TankSolutionName, out var tankSolution, out _))
            return;
        if (!_solutionContainerSystem.TryGetSolution(ent.Owner, ent.Comp.NozzleSolutionName, out var nozzleSolution, out _))
            return;

        if (tankSolution != null)
        {
            var splitedSolution = _solutionContainerSystem.SplitSolution(tankSolution.Value, 10f);
            _solutionContainerSystem.AddSolution(nozzleSolution.Value, splitedSolution);
        }
    }

    private void OnClothingSolutionCount(EntityUid uid, ClothingSlotSolutionProviderComponent component, ref GetSolutionCountEvent args)
    {
        if (!TryGetClothingSlotEntity(uid, component, out var entity))
            return;
        RaiseLocalEvent(entity.Value, ref args);
    }

    private bool TryGetClothingSlotEntity(EntityUid uid, ClothingSlotSolutionProviderComponent component, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        slotEntity = null;

        if (!_container.TryGetContainingContainer((uid, null, null), out var container))
            return false;
        var user = container.Owner;

        if (!_inventory.TryGetContainerSlotEnumerator(user, out var enumerator, component.SolutionRequiredSlot))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (_whitelistSystem.IsWhitelistFailOrNull(component.SolutionProviderWhitelist, item))
                continue;

            slotEntity = item;
            return true;
        }

        return false;
    }
    private void UpdateTankAppearance(EntityUid uid, SolutionProviderComponent tank)
    {
        Appearance.SetData(uid, TankVisuals.NozzleInserted, tank.ContainedNozzle != null);
    }
}
