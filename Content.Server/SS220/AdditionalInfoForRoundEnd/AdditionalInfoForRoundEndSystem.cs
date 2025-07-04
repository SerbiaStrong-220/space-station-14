using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.SS220.AdditionalInfoForRoundEnd;
using Robust.Shared.Utility;

namespace Content.Server.SS220.AdditionalInfoForRoundEnd;

/// <summary>
/// Sends and organizes server-side round end summary data for display on the client.
/// Gathers statistics from IRoundEndInfoDisplay sources and broadcasts them
/// as structured blocks. Also handles antagonist purchase info.
/// </summary>
public sealed class AdditionalInfoForRoundEndSystem : EntitySystem
{
    [Dependency] private readonly RoundEndInfoManager _infoManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnd);
    }

    /// <summary>
    /// Clears all stored round end data when the round ends.
    /// </summary>
    private void OnRoundEnd(RoundEndedEvent args)
    {
        _infoManager.ClearAllDatas();
    }

    /// <summary>
    /// Compiles antagonist item purchases from AntagPurchaseInfo and sends them
    /// to all clients via a network event.
    /// </summary>
    public void SendAntagInfo()
    {
        var info = _infoManager.EnsureInfo<AntagPurchaseInfo>()!;

        var ev = new RoundEndAntagItemsEvent();

        foreach (var allPurchase in info.GetAllPurchases())
        {
            ev.PlayerPurchases.Add(new RoundEndAntagPurchaseData
            {
                Name = allPurchase.Key,
                ItemPrototypes = allPurchase.Value.ItemPrototypes,
                TotalTC = allPurchase.Value.TotalTC,
            });
        }

        RaiseNetworkEvent(ev);
    }

    /// <summary>
    /// Collects all IRoundEndInfoDisplay entries, groups them by category, formats their text content,
    /// and sends the aggregated result to clients for display in the round end summary UI.
    /// </summary>
    public void SendAdditionalInfo()
    {
        var groups = _infoManager.GetAllInfos()
            .OfType<IRoundEndInfoDisplay>()
            .OrderBy(info => info.DisplayOrder)
            .GroupBy(info => info.DisplayOrder / 100);

        var blocks = new List<RoundEndInfoDisplayBlock>();

        foreach (var group in groups)
        {
            var builder = new FormattedMessage();
            string? title = null;
            Color? color = null;

            foreach (var display in group)
            {
                title ??= display.Title;
                display.AddSummaryText(builder);
                builder.PushNewline();
                color ??= display.BackgroundColor;
            }

            if (!builder.IsEmpty)
            {
                blocks.Add(new RoundEndInfoDisplayBlock
                {
                    Title = title ?? Loc.GetString("additional-info-no-category"),
                    Body = builder.ToMarkup(),
                    Color = color ?? new Color(30, 30, 30, 200),
                });
            }
        }

        RaiseNetworkEvent(new RoundEndAdditionalInfoEvent
        {
            AdditionalInfo = blocks,
        });
    }
}
