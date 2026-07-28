// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed partial class ItemExtensionSystem : EntitySystem
{
    private static readonly LocId CannotPickupMessage = "too-heavy-cant-pick-up";

    [Dependency] private PhysicalParametersSystem _parametersSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedWieldableSystem _wield = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemExtensionComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<ItemExtensionComponent, UserParametersChangedEvent>(OnUserParametersChanged);
        SubscribeLocalEvent<ItemExtensionComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<ItemExtensionComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<ItemExtensionComponent, GotUnequippedHandEvent>(OnUnequipped);

        base.Initialize();
    }

    public void OnPickupAttempt(Entity<ItemExtensionComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.User, out var parametersComp))
            userStrength = _parametersSystem.GetParameterValue((args.User, parametersComp), Parameter.Strength);

        FixedPoint2 usedStrength = userStrength;

        FixedPoint2 totalFreeStrength = usedStrength;

        var activeHandIdNullable = _hands.GetActiveHand(args.User);

        if (activeHandIdNullable == null)
            return;

        string activeHandId = (string)activeHandIdNullable;

        if (_hands.TryGetHand(args.User, activeHandId, out var activeHand) &&
            activeHand.Value.HandOverride != null)
            usedStrength = activeHand.Value.HandOverride.Value.StrengthModifier;

        if (usedStrength >= ent.Comp.MinimalStrengthToPickUp)
            return;

        foreach (var handId in _hands.EnumerateHands(args.User))
        {
            if (handId == activeHandId)
                continue;

            if (_hands.HandIsEmpty(args.User, handId) &&
                _hands.TryGetHand(args.User, handId, out var freeHand))
            {
                if (freeHand.Value.HandOverride == null)
                {
                    totalFreeStrength += userStrength;
                    continue;
                }
                totalFreeStrength += freeHand.Value.HandOverride.Value.StrengthModifier;
            }
        }

        if (totalFreeStrength < ent.Comp.MinimalStrengthToPickUp)
        {
            args.Cancel();
            _popup.PopupClient(Loc.GetString(CannotPickupMessage), args.User);
            return;
        }
    }

    public void OnUserParametersChanged(Entity<ItemExtensionComponent> ent, ref UserParametersChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.User, out var parametersComp))
            userStrength = _parametersSystem.GetParameterValue((args.User, parametersComp), Parameter.Strength);

        FixedPoint2 usedStrength = GetStrength(args.User, ent.Owner);

        if (usedStrength >= ent.Comp.MinimalStrengthToPickUp)
            return;

        if (userStrength < ent.Comp.MinimalStrengthToPickUp)
        {
            FixedPoint2 totalFreeStrength = usedStrength;
            Dictionary<FixedPoint2, string> freeHands = new Dictionary<FixedPoint2, string>();

            foreach (var handId in _hands.EnumerateHands(args.User))
            {
                if (_hands.HandIsEmpty(args.User, handId) &&
                    _hands.TryGetHand(args.User, handId, out var freeHand))
                {
                    if (freeHand.Value.HandOverride == null)
                    {
                        freeHands.Add(userStrength, handId);
                        totalFreeStrength += userStrength;
                        continue;
                    }

                    freeHands.Add(freeHand.Value.HandOverride.Value.StrengthModifier, handId);
                    totalFreeStrength += freeHand.Value.HandOverride.Value.StrengthModifier;
                }
            }

            if (totalFreeStrength < ent.Comp.MinimalStrengthToPickUp)
            {
                _hands.TryDrop(args.User, ent.Owner, checkActionBlocker: false);
                _popup.PopupClient(Loc.GetString(CannotPickupMessage), args.User);
            }

            var sortedHands = freeHands
                .OrderByDescending(pair => pair.Key)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            foreach (var (handStrength, handId) in sortedHands)
            {
                usedStrength += handStrength;

                _virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.User, empty: handId, virtualItem: out var _);

                if (usedStrength >= ent.Comp.MinimalStrengthToPickUp)
                    return;
            }
        }
    }

    public FixedPoint2 GetStrength(EntityUid user, EntityUid used)
    {
        var handsDict = GetUsedHands(user, used);

        FixedPoint2 resultStrength = 0;

        FixedPoint2 defaultUserParameter = 1;

        if (TryComp<PhysicalParametersComponent>(user, out var parametersComp))
            defaultUserParameter = _parametersSystem.GetParameterValue((user, parametersComp), Parameter.Strength);

        foreach (var (key, value) in handsDict)
        {
            if (value.HandOverride == null)
            {
                resultStrength += defaultUserParameter;
                continue;
            }

            resultStrength += value.HandOverride.Value.StrengthModifier;
        }

        return resultStrength;
    }

    public Dictionary<string, Hand> GetUsedHands(EntityUid user, EntityUid used)
    {
        var handsUsed = new Dictionary<string, Hand>();

        if (!TryComp<HandsComponent>(user, out var handsComp))
            return handsUsed;

        if (_container.TryGetContainingContainer(used, out var container) &&
                _hands.TryGetHand(user, container.ID, out var usedHand))
        {
            handsUsed.Add(container.ID, (Hand)usedHand);
        }

        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (TryComp(held, out VirtualItemComponent? virt) &&
                virt.BlockingEntity == used &&
                _container.TryGetContainingContainer(used, out var containerHand) &&
                !handsUsed.ContainsKey(containerHand.ID) &&
                _hands.TryGetHand(user, containerHand.ID, out var handHoldingVirtual))
            {
                handsUsed.Add(containerHand.ID, handHoldingVirtual.Value);
            }
        }

        return handsUsed;
    }

    public Dictionary<string, Hand> OccupyHands(EntityUid user, EntityUid used)
    {
        var handsUsed = new Dictionary<string, Hand>();

        if (!TryComp<HandsComponent>(user, out var handsComp))
            return handsUsed;

        if (_container.TryGetContainingContainer(used, out var container) &&
                _hands.TryGetHand(user, container.ID, out var usedHand))
        {
            handsUsed.Add(container.ID, usedHand.Value);
        }

        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (TryComp(held, out VirtualItemComponent? virt) &&
                virt.BlockingEntity == used &&
                _container.TryGetContainingContainer(used, out var containerHand) &&
                _hands.TryGetHand(user, containerHand.ID, out var handHoldingVirtual))
            {
                handsUsed.Add(containerHand.ID, handHoldingVirtual.Value);
            }
        }

        return handsUsed;
    }

    private void OnEquipped(Entity<ItemExtensionComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.User, out var parametersComp))
            userStrength = _parametersSystem.GetParameterValue((args.User, parametersComp), Parameter.Strength);

        FixedPoint2 usedStrength = userStrength;

        var activeHandIdNullable = _hands.GetActiveHand(args.User);

        if (activeHandIdNullable == null)
            return;

        string activeHandId = (string)activeHandIdNullable;

        if (_hands.TryGetHand(args.User, activeHandId, out var activeHand) &&
            activeHand.Value.HandOverride != null)
            usedStrength = activeHand.Value.HandOverride.Value.StrengthModifier;

        if (usedStrength >= ent.Comp.MinimalStrengthToPickUp)
            return;

        if (TryComp<WieldableComponent>(ent.Owner, out var wieldableComp) &&
            _wield.TryWield(ent.Owner, wieldableComp, args.User))
            usedStrength = GetStrength(ent.Owner, args.User);

        if (usedStrength >= ent.Comp.MinimalStrengthToPickUp)
            return;

        Dictionary<FixedPoint2, string> freeHands = new Dictionary<FixedPoint2, string>();

        foreach (var handId in _hands.EnumerateHands(args.User))
        {
            if (handId == activeHandId)
                continue;

            if (_hands.HandIsEmpty(args.User, handId) &&
                _hands.TryGetHand(args.User, handId, out var freeHand))
            {
                if (freeHand.Value.HandOverride == null)
                {
                    freeHands.Add(userStrength, handId);
                    continue;
                }

                freeHands.Add(freeHand.Value.HandOverride.Value.StrengthModifier, handId);
            }
        }

        var sortedHands = freeHands
            .OrderByDescending(pair => pair.Key)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var (handStrength, handId) in sortedHands)
        {
            usedStrength += handStrength;

            _virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.User, empty: handId, virtualItem: out var _);

            if (usedStrength >= ent.Comp.MinimalStrengthToPickUp)
                return;
        }
    }

    private void OnUnequipped(Entity<ItemExtensionComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _virtualItem.DeleteInHandsMatching(args.User, ent.Owner);
    }

    private void OnVirtualItemDeleted(Entity<ItemExtensionComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.BlockingEntity != ent.Owner || _timing.ApplyingState)
            return;

        if (GetStrength(args.User, ent.Owner) > ent.Comp.MinimalStrengthToPickUp)
            return;

        _hands.TryDrop(args.User, ent.Owner);
    }
}
