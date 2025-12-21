using Content.Shared.Store;

namespace Content.Shared.SS220.Store.Listing;

/// <summary>
/// Base event triggered when a store listing is purchased.
/// Inheritors can define specific effects or logic tied to the purchase.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class ListingPurchasedEvent : EntityEventArgs
{
    /// <summary>
    /// Unique identifier of the purchased listing.
    /// Used to resolve listing metadata, pricing, and effects.
    /// </summary>
    public ListingDataWithCostModifiers Listing;

    /// <summary>
    /// Entity that initiated the purchase.
    /// </summary>
    public EntityUid Purchaser;

    /// <summary>
    /// Entity representing the store where the purchase occurred.
    /// </summary>
    public EntityUid StoreUid;
}
