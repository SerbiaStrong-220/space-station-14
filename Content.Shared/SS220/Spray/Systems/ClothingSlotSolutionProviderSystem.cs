// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Inventory;
using Content.Shared.SS220.Spray.Components;
using Content.Shared.SS220.Spray.Events;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Spray.Systems;

public sealed class ClothingSlotSolutionProviderSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public bool TryGetClothingSolution(Entity<ClothingSlotSolutionProviderComponent> ent,
        [NotNullWhen(true)] out Entity<SolutionComponent>? soln,
        [NotNullWhen(true)] out Solution? solution)
    {
        soln = default;
        solution = default;
        if (!TryGetClothingSlotEntity(ent.Owner, ent.Comp, out var tank))
            return false;
        if (!_solutionContainerSystem.TryGetSolution(tank.Value, ent.Comp.TankSolutionName, out soln, out solution))
            return false;
        return true;
    }

    private bool TryGetClothingSlotEntity(EntityUid uid,
        ClothingSlotSolutionProviderComponent component,
        [NotNullWhen(true)] out EntityUid? slotEntity)
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
}
