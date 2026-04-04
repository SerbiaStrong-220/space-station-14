using Content.Server.Cargo.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.SS220.Overlays;

namespace Content.Server.SS220.Overlays;

public sealed class ShowSupplyPriceSystem : EntitySystem
{
    [Dependency] private readonly PricingSystem _pricingSystem = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;
    [Dependency] private readonly CargoSystem _bountySystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MetaDataComponent, ExaminedEvent>(OnExamined);
    }

    private bool IsWearing(EntityUid uid)
    {
        return _invSystem.TryGetSlotEntity(uid, "eyes", out var huds)
               && HasComp<ShowSupplyPriceComponent>(huds);
    }

    private void OnExamined(Entity<MetaDataComponent> ent, ref ExaminedEvent args)
    {
        if (!IsWearing(args.Examiner))
            return;

        var price = Math.Round(_pricingSystem.GetPrice(args.Examined), 2); // price = ,**

        if (price == 0)
            return;

        if (_bountySystem.IsBountyComplete(args.Examined, out _))
        {
            var msgBountyComplete = Loc.GetString($"supply-hud-bounty-complete", ("price", price));
            args.PushMarkup(msgBountyComplete);
        }

        var msg = Loc.GetString($"supply-hud-total-price", ("price", price));
        args.PushMarkup(msg);
    }

}
