// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.SS220.ItemExtension;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class ItemExtensionSystem : EntitySystem
{
    private static readonly LocId CannotPickupMessage = "too-heavy-cant-pick-up";

    [Dependency] private readonly PhysicalParametersSystem _parametersSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemExtensionComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<ItemExtensionComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<ItemExtensionComponent, GotUnequippedHandEvent>(OnUnequipped);

        base.Initialize();
    }

    public void OnPickupAttempt(Entity<ItemExtensionComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
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

    private void OnEquipped(Entity<ItemExtensionComponent> ent, ref GotEquippedHandEvent args)
    {
        FixedPoint2 userStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.User, out var parametersComp))
            userStrength = _parametersSystem.GetParameterValue((args.User, parametersComp), Parameter.Strength);

        for (var i = 0; i < ent.Comp.MinimalStrengthToPickUp / userStrength - 1; i++)
            _virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.User);
    }

    private void OnUnequipped(Entity<ItemExtensionComponent> ent, ref GotUnequippedHandEvent args)
    {
        _virtualItem.DeleteInHandsMatching(args.User, ent.Owner);
    }
}
