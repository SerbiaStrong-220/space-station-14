// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class ItemExtensionSystem : EntitySystem
{
    private static readonly LocId CannotPickupMessage = "too-heavy-cant-pick-up";

    [Dependency] private readonly PhysicalParametersSystem _parametersSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedWieldableSystem _wield = default!;

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

        if (userStrength < ent.Comp.MinimalStrengthToPickUp)
        {
            if (parametersComp == null)
            {
                args.Cancel();
                _popup.PopupClient(Loc.GetString(CannotPickupMessage), args.User);
                return;
            }

            if (parametersComp.StrengthAffectsArms)
            {
                if (_hands.CountFreeHands(args.User) * userStrength < ent.Comp.MinimalStrengthToPickUp || userStrength == 0)
                {
                    args.Cancel();
                    _popup.PopupClient(Loc.GetString(CannotPickupMessage), args.User);
                    return;
                }

                return;
            }
            args.Cancel();
            _popup.PopupClient(Loc.GetString(CannotPickupMessage), args.User);
        }
    }

    public void OnUserParametersChanged(Entity<ItemExtensionComponent> ent, ref UserParametersChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.User, out var parametersComp))
            userStrength = _parametersSystem.GetParameterValue((args.User, parametersComp), Parameter.Strength);

        if (userStrength < ent.Comp.MinimalStrengthToPickUp)
        {
            if (parametersComp == null)
            {
                _hands.TryDrop(args.User, ent.Owner, checkActionBlocker: false);
                _virtualItem.DeleteInHandsMatching(args.User, ent.Owner);
                _popup.PopupClient(Loc.GetString(CannotPickupMessage), args.User);
                return;
            }

            if (parametersComp.StrengthAffectsArms)
            {
                if (_hands.CountFreeHands(args.User) * userStrength < ent.Comp.MinimalStrengthToPickUp || userStrength == 0)
                {
                    _hands.TryDrop(args.User, ent.Owner, checkActionBlocker: false);
                    _popup.PopupClient(Loc.GetString(CannotPickupMessage), args.User);
                    return;
                }

                _virtualItem.DeleteInHandsMatching(args.User, ent.Owner);

                int handsUsedInWielding = 0;

                if (TryComp<WieldableComponent>(ent.Owner, out var wieldableComponent) &&
                    !wieldableComponent.Wielded &&
                    wieldableComponent.FreeHandsRequired <= ent.Comp.MinimalStrengthToPickUp / userStrength &&
                    _wield.TryWield(ent.Owner, wieldableComponent, args.User))
                    handsUsedInWielding += wieldableComponent.FreeHandsRequired - 1;

                if (wieldableComponent != null && wieldableComponent.Wielded)
                    handsUsedInWielding += wieldableComponent.FreeHandsRequired - 1;

                for (var i = 0; i < ent.Comp.MinimalStrengthToPickUp / userStrength - 1 - handsUsedInWielding; i++)
                    _virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.User);

                return;
            }
            _hands.TryDrop(args.User, ent.Owner, checkActionBlocker: false);
            _popup.PopupClient(Loc.GetString(CannotPickupMessage), args.User);
        }
        _virtualItem.DeleteInHandsMatching(args.User, ent.Owner);
    }

    public int TryGetNeededAmountOfHands(EntityUid user, EntityUid used)
    {
        if (!TryComp<ItemExtensionComponent>(used, out var itemComp))
            return 1;

        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(user, out var parametersComp))
            userStrength = _parametersSystem.GetParameterValue((user, parametersComp), Parameter.Strength);

        if (userStrength < itemComp.MinimalStrengthToPickUp)
        {
            if (parametersComp == null)
                return -1;

            if (parametersComp.StrengthAffectsArms)
            {
                int neededHands = (itemComp.MinimalStrengthToPickUp / userStrength).Int();

                if ((itemComp.MinimalStrengthToPickUp / userStrength) != ((itemComp.MinimalStrengthToPickUp / userStrength).Int()))
                    neededHands += 1;

                return neededHands;
            }
        }

        return 1;
    }

    private void OnEquipped(Entity<ItemExtensionComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.User, out var parametersComp))
            userStrength = _parametersSystem.GetParameterValue((args.User, parametersComp), Parameter.Strength);

        int handsUsedInWielding = 0;

        if (TryComp<WieldableComponent>(ent.Owner, out var wieldableComponent) &&
            !wieldableComponent.Wielded &&
            wieldableComponent.FreeHandsRequired <= ent.Comp.MinimalStrengthToPickUp / userStrength &&
            _wield.TryWield(ent.Owner, wieldableComponent, args.User))
            handsUsedInWielding += wieldableComponent.FreeHandsRequired - 1;

        if (wieldableComponent != null && wieldableComponent.Wielded)
            handsUsedInWielding += wieldableComponent.FreeHandsRequired - 1;

        for (var i = 0; i < ent.Comp.MinimalStrengthToPickUp / userStrength - 1 - handsUsedInWielding; i++)
            _virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.User);
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

        if (TryGetNeededAmountOfHands(args.User, ent.Owner) == 1)
            return;

        _hands.TryDrop(args.User, ent.Owner);
    }
}
