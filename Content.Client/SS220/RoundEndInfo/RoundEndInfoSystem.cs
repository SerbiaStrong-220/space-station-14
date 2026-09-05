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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundEndAdditionalInfoEvent>(RoundEndAdditionalInfo);
    }

    private void RoundEndAdditionalInfo(RoundEndAdditionalInfoEvent message)
    {
        foreach (var block in message.AdditionalInfo)
        {
            switch (block)
            {
                case RoundEndAntagPurchaseData purchaseData:
                    _userInterfaceManager
                        .GetUIController<RoundEnd.RoundEndSummaryUIController>()
                        .PopulateAdditionalInfo(purchaseData);
                    break;

                case RoundEndInfoDisplayBlock displayBlock:
                    _userInterfaceManager
                        .GetUIController<RoundEnd.RoundEndSummaryUIController>()
                        .PopulateAdditionalInfo(displayBlock);
                    break;
            }
        }
    }
}
