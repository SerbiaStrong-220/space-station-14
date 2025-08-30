using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.SS220.RoundEndInfo;
using Robust.Shared.Utility;

namespace Content.Server.SS220.RoundEndInfo;

/// <summary>
/// Sends and organizes server-side round end summary data for display on the client.
/// Gathers statistics from IRoundEndInfoDisplay sources and broadcasts them
/// as structured blocks. Also handles antagonist purchase info.
/// </summary>
public sealed class RoundEndInfoSystem : SharedRoundEndInfoSystem
{
    [Dependency] private readonly ISharedRoundEndInfoManager _infoManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnd);
    }

    /// <summary>
    /// Clears all stored round end data when the round ends.
    /// </summary>
    private void OnRoundEnd(RoundEndedEvent args)
    {
        SendAdditionalInfo();
        _infoManager.ClearAllData();
    }

    /// <summary>
    /// Compiles antagonist item purchases from AntagPurchaseInfo and sends them
    /// to all clients via a network event.
    /// </summary>
    private void SendAntagInfo(List<IRoundEndInfoData> blocks)
    {
        var info = _infoManager.EnsureInfo<AntagPurchaseInfo>()!;
        var purchaseList = new List<RoundEndAntagPurchaseData>();

        foreach (var allPurchase in info.GetAllPurchases())
        {
            if (!TryComp(allPurchase.Key, out MindComponent? mind)
                || string.IsNullOrEmpty(mind.CharacterName))
                continue;

            purchaseList.Add(new RoundEndAntagPurchaseData
            {
                Name = mind.CharacterName,
                ItemPrototypes = allPurchase.Value.ItemPrototypes,
                TotalTC = allPurchase.Value.TotalTC,
            });
        }

        blocks.AddRange(purchaseList.OrderByDescending(p => p.TotalTC));
    }

    /// <summary>
    /// Collects all IRoundEndInfoDisplay entries, groups them by category, formats their text content,
    /// and sends the aggregated result to clients for display in the round end summary UI.
    /// </summary>
    private void SendAdditionalInfo()
    {
        var groups = _infoManager.GetAllInfos()
            .OfType<IRoundEndInfoDisplay>()
            .OrderBy(info => info.DisplayOrder)
            .GroupBy(info => info.DisplayOrder / 100);

        var blocks = new List<IRoundEndInfoData>();

        // we need to add antag blocks firstly, cause antag info must be on top
        SendAntagInfo(blocks);

        foreach (var group in groups)
        {
            var builder = new FormattedMessage();
            string? title = null;
            Color? color = null;

            foreach (var display in group)
            {
                title ??= display.Title;
                builder.AddMessage(display.GetSummaryText());
                builder.PushNewline();
                color ??= display.BackgroundColor;
            }

            if (builder.IsEmpty)
                continue;

            var displayBlock = new RoundEndInfoDisplayBlock
            {
                Title = title ?? Loc.GetString("additional-info-no-category"),
                Body = builder.ToMarkup(),
                Color = color ?? new Color(30, 30, 30, 200),
            };

            blocks.Add(displayBlock);
        }

        RaiseNetworkEvent(new RoundEndAdditionalInfoEvent
        {
            AdditionalInfo = blocks,
        });
    }
}
