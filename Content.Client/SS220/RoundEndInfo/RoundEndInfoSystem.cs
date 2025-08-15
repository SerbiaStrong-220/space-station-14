using System.Linq;
using Content.Shared.SS220.RoundEndInfo;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.RoundEndInfo;

/// <summary>
/// Handles reception and storage of antagonist item purchases sent at round end.
/// This system listens for the RoundEndAntagItemsEvent and updates the client data accordingly,
/// so it can be displayed in the round end UI.
/// </summary>
public sealed class RoundEndInfoSystem : SharedRoundEndInfoSystem
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    /// <summary>
    /// Holds round-end client-side data related to antagonist purchases.
    /// Used for displaying round summary UI information.
    /// </summary>
    private List<RoundEndAntagPurchaseData> _antagItems = new();
    public IReadOnlyList<RoundEndAntagPurchaseData> AntagItems => _antagItems;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundEndAntagItemsEvent>(OnRoundEndAntagItems);
        SubscribeNetworkEvent<RoundEndAdditionalInfoEvent>(RoundEndAdditionalInfo);
    }

    /// <summary>
    /// Handles the RoundEndAntagItemsEvent received from the server.
    /// Clears any existing data and populates AntagItems with new sorted player purchases.
    /// </summary>
    /// <param name="args">The event containing player antagonist purchase data.</param>
    private void OnRoundEndAntagItems(RoundEndAntagItemsEvent args)
    {
        _antagItems.Clear();

        if (args.PlayerPurchases.Count == 0)
            return;

        var sorted = args.PlayerPurchases
            .OrderByDescending(p => p.TotalTC)
            .ToList();

        _antagItems = sorted;
    }

    private void RoundEndAdditionalInfo(RoundEndAdditionalInfoEvent message)
    {
        _userInterfaceManager.GetUIController<RoundEnd.RoundEndSummaryUIController>().PopulateAdditionalInfo(message);
    }
}
