using System.Linq;
using Content.Shared.SS220.AdditionalInfoForRoundEnd;

namespace Content.Client.SS220.AdditionalInfoForRoundEnd;

/// <summary>
/// Handles reception and storage of antagonist item purchases sent at round end.
/// This system listens for the RoundEndAntagItemsEvent and updates the client data accordingly,
/// so it can be displayed in the round end UI.
/// </summary>
public sealed class AdditionalInfoForRoundEndSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeNetworkEvent<RoundEndAntagItemsEvent>(OnRoundEndAntagItems);
    }

    /// <summary>
    /// Handles the RoundEndAntagItemsEvent received from the server.
    /// Clears any existing data and populates AntagItems with new sorted player purchases.
    /// </summary>
    /// <param name="args">The event containing player antagonist purchase data.</param>
    private void OnRoundEndAntagItems(RoundEndAntagItemsEvent args)
    {
        RoundEndClientData.AntagItems.Clear();

        var sorted = args.PlayerPurchases
            .OrderByDescending(p => p.TotalTC)
            .ToList();

        foreach (var player in sorted)
        {
            RoundEndClientData.AntagItems[player.Name] = new AntagPurchaseClientData
            {
                Items = player.ItemPrototypes,
                TotalTC = player.TotalTC,
            };
        }
    }
}

/// <summary>
/// Holds round-end client-side data related to antagonist purchases.
/// Used for displaying round summary UI information.
/// </summary>
public static class RoundEndClientData
{
    public static readonly Dictionary<string, AntagPurchaseClientData> AntagItems = new();
}

/// <summary>
/// Contains the client-side data for a single player's antagonist purchases,
/// including the list of item prototype IDs and total Telecrystals spent.
/// </summary>
public sealed class AntagPurchaseClientData
{
    /// <summary>
    /// List of antagonist item prototype IDs purchased by the player.
    /// </summary>
    public List<string> Items = new();

    /// <summary>
    /// Total amount of Telecrystals spent by the player.
    /// </summary>
    public int TotalTC;
}
