using System.Linq;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Store.Conditions;

public sealed partial class BuyBeforeCondition : ListingCondition
{
    //ss220 blocking all listings, if any other listing was bought start
    /// <summary>
    ///     Required listing(s) needed to purchase before this listing is available
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ListingPrototype>>? Whitelist;

    /// <summary>
    /// If true, block to buy listing if any other listing was bought
    /// </summary>
    [DataField]
    public bool BlacklistAll;
    //ss220 blocking all listings, if any other listing was bought end

    /// <summary>
    ///     Listing(s) that if bought, block this purchase, if any.
    /// </summary>
    public HashSet<ProtoId<ListingPrototype>>? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        if (!args.EntityManager.TryGetComponent<StoreComponent>(args.StoreEntity, out var storeComp))
            return false;

        var allListings = storeComp.FullListingsCatalog;

        //ss220 blocking all listings, if any other listing was bought start
        if (BlacklistAll)
        {
            if (allListings.Any(listing => listing.PurchaseAmount > 0))
                return false;
        }

        var purchasesFound = true;
        //ss220 blocking all listings, if any other listing was bought end

        if (Blacklist != null)
        {
            foreach (var blacklistListing in Blacklist)
            {
                foreach (var listing in allListings)
                {
                    if (listing.ID == blacklistListing.Id && listing.PurchaseAmount > 0)
                        return false;
                }
            }
        }

        //ss220 blocking all listings, if any other listing was bought start
        if (Whitelist != null)
        {
            foreach (var requiredListing in Whitelist)
            {
                foreach (var listing in allListings)
                {
                    if (listing.ID == requiredListing.Id)
                    {
                        purchasesFound = listing.PurchaseAmount > 0;
                        break;
                    }
                }
            }
        }
        //ss220 blocking all listings, if any other listing was bought end

        return purchasesFound;
    }
}
