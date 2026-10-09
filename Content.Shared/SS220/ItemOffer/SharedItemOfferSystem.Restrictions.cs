using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.StuckOnEquip;

namespace Content.Shared.SS220.ItemOffer;

public abstract partial class SharedItemOfferSystem
{
    private void InitializeRestrictions()
    {
        SubscribeLocalEvent((EntityUid _, VirtualItemComponent _, ref CanOfferItemEvent args) => args.Cancelled = true);
        SubscribeLocalEvent((EntityUid _, UnremoveableComponent _, ref CanOfferItemEvent args) => args.Cancelled = true);
        SubscribeLocalEvent((EntityUid _, StuckOnEquipComponent comp, ref CanOfferItemEvent args) => args.Cancelled = comp.IsStuck);

        SubscribeLocalEvent((EntityUid _, BorgChassisComponent _, ref CanOfferItemEvent args) => args.Cancelled = true);
    }
}
