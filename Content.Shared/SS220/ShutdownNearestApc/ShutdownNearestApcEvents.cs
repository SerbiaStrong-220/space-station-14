using Content.Shared.SS220.Store.Listing;

namespace Content.Shared.SS220.ShutdownNearestApc;

/// <summary>
/// Listing effect that shuts down the nearest APC within a given radius.
/// Triggered when a specific store listing is purchased.
/// </summary>
public sealed partial class ShutdownNearestApcEvent : ListingPurchasedEvent
{
    /// <summary>
    /// Radius within which the nearest APC will be targeted.
    /// </summary>
    [DataField]
    public float Radius;
}
