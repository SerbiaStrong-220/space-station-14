using System.Linq;
using Content.Server.Administration.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.SS220.RoundEndInfo;
using Robust.Shared.Utility;

namespace Content.Server.SS220.RoundEndInfo;

/// <summary>
/// Sends and organizes server-side round end summary data for display on the client.
/// Gathers statistics from <see cref="IRoundEndInfoDisplay"/> sources and broadcasts them
/// as structured blocks. Also handles <see cref="AntagPurchaseInfo"/>.
/// </summary>
public sealed class RoundEndInfoSystem : SharedRoundEndInfoSystem
{
    [Dependency] private readonly AdminTestArenaSystem _arena = default!;
    [Dependency] private readonly IRoundEndInfoManager _infoManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundEndAdditionalInfoCheckMapEvent>(OnMapCheck);
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
    /// Clears all stored data when the round starts.
    /// </summary>
    private void OnRoundStart(RoundStartedEvent args)
    {
        _infoManager.ClearAllData();
    }

    /// <summary>
    /// Handles <see cref="RoundEndAdditionalInfoCheckMapEvent"/> to determine whether a player
    /// should be excluded from round-end statistics based on their current map.
    /// </summary>
    private void OnMapCheck(ref RoundEndAdditionalInfoCheckMapEvent args)
    {
        if (args.User == null || !Exists(args.User) || TerminatingOrDeleted(args.User))
        {
            args.Cancelled = true;
            return;
        }

        var user = args.User.Value;

        if (TryComp<MindComponent>(args.User, out var mind) && mind.CurrentEntity != null)
            user = mind.CurrentEntity.Value;

        var xform = Transform(user);

        if (_arena.ArenaMap.ToList().All(kvp => kvp.Value != xform.MapUid))
            return;

        args.Cancelled = true;
    }

    /// <summary>
    /// Compiles antagonist item purchases from <see cref="AntagPurchaseInfo"/> and sends them
    /// to all clients via a network event <see cref="RoundEndAdditionalInfoEvent"/>.
    /// </summary>
    private void AddAntagPurchaseInfo(List<IRoundEndInfoData> blocks)
    {
        if (!_infoManager.TryGetInfo<AntagPurchaseInfo>(out var info))
            return;

        var purchaseList = new List<RoundEndAntagPurchaseData>();

        foreach (var allPurchase in info.Purchases)
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
    /// Collects all <see cref="IRoundEndInfoDisplay"/> entries, groups them by category, formats their text content,
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
        AddAntagPurchaseInfo(blocks);

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
