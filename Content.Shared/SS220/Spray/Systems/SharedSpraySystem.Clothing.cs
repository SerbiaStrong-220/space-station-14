using System.Diagnostics.CodeAnalysis;
using Content.Shared.Inventory;
using Content.Shared.SS220.Spray.Components;
using Content.Shared.SS220.Spray.Events;
using Content.Shared.Whitelist;
using Linguini.Bundle.Errors;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Spray.System;

public sealed partial class SharedSpraySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClothingSlotSolutionProviderComponent, TakeSolutionEvent>(OnClothingTakeSolution);
        SubscribeLocalEvent<ClothingSlotSolutionProviderComponent, GetSolutionCountEvent>(OnClothingSolutionCount);
    }

    private void OnClothingTakeSolution(EntityUid uid, ClothingSlotSolutionProviderComponent component, TakeSolutionEvent args)
    {
        if (!TryGetClothingSlotEntity(uid, component, out var entity))
            return;
        RaiseLocalEvent(entity.Value, args);
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

        if (!_container.TryGetContainingContainer(uid, out var container))
            return false;
        var user = container.Owner;

        if (!_inventory.TryGetContainerSlotEnumerator(user, out var enumerator, component.SolutionRequiredSlot))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (component.SolutionProviderWhitelist == null ||
            !_whitelistSystem.IsValid(component.SolutionProviderWhitelist, uid))
                continue;

            slotEntity = item;
            return true;
        }

        return false;
    }
}

