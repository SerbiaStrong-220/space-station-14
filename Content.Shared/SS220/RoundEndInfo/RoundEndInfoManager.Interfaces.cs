using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.RoundEndInfo;

/// <summary>
/// Base interface for any data provider that contributes to round-end summaries.
/// </summary>
public interface IRoundEndInfo;

/// <summary>
/// Interface for info types that contribute displayable content to the round-end UI.
/// </summary>
public interface IRoundEndInfoDisplay : IRoundEndInfo
{
    Color? BackgroundColor => null;

    /// <summary>
    /// Determines the relative sort order of this info block in the round-end summary.
    /// </summary>
    int DisplayOrder { get; }

    /// <summary>
    /// The title used to label this block in the summary UI.
    /// </summary>
    LocId Title { get; }

    /// <summary>
    /// Adds formatted text content to the summary using the provided message builder.
    /// </summary>
    FormattedMessage GetSummaryText();
}

/// <summary>
/// Generic base class for round-end info types that track data per player.
/// Provides helper methods for recording, summarizing, and ranking data.
/// </summary>
/// <typeparam name="TState">The per-player data structure used by this info type.</typeparam>
public abstract class RoundEndInfoBase<TState> : IRoundEndInfoDisplay where TState : struct, IBaseInfoState
{
    [Dependency] protected readonly IEntityManager EntMan = default!;

    /// <summary>
    /// Internal per-player data store, mapping each player’s mind entity to their statistics.
    /// </summary>
    protected readonly Dictionary<EntityUid, TState> Data = new();

    public abstract int DisplayOrder { get; }
    public abstract LocId Title { get; }
    public abstract FormattedMessage GetSummaryText();

    /// <summary>
    /// Called when a valid player record event occurs.
    /// Override this method to update the provided state structure.
    /// </summary>
    protected abstract void RecordInternal(ref TState state);

    /// <summary>
    /// Records data for a specific player, if allowed by event checks. (e.g., admin test arena)
    /// </summary>
    public void Record(EntityUid userMind)
    {
        var ev = new RoundEndAdditionalInfoCheckMapEvent(userMind);
        EntMan.EventBus.RaiseLocalEvent(userMind, ref ev, true);
        if (ev.Cancelled)
            return;

        var state = Data.GetValueOrDefault(userMind);

        RecordInternal(ref state);
        Data[userMind] = state;
    }

    /// <summary>
    /// Returns the entity with the highest value based on a selector function.
    /// </summary>
    protected (EntityUid?, int) GetTop(Func<TState, int> selector)
    {
        return RoundEndInfoUtils.GetTopBy(Data, selector);
    }

    /// <summary>
    /// Returns the total sum of a given selector across all tracked players.
    /// </summary>
    protected int GetTotal(Func<TState, int> selector)
    {
        return Data.Sum(pair => selector(pair.Value));
    }
}

/// <summary>
/// Stores and aggregates antagonist item purchases during the round.
/// Each purchase is tracked per player and contributes to the round-end summary.
/// </summary>
public sealed class AntagPurchaseInfo : IRoundEndInfo
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    /// <summary>
    /// All recorded antagonist purchases, grouped by player.
    /// </summary>
    public IReadOnlyDictionary<EntityUid, RoundEndAntagPurchaseData> Purchases => _purchases;

    private readonly Dictionary<EntityUid, RoundEndAntagPurchaseData> _purchases = new();

    /// <summary>
    /// Records a purchased antagonist item for the given user.
    /// </summary>
    /// <param name="userMind">The mind entity that made the purchase.</param>
    /// <param name="itemName">The prototype ID of the purchased item.</param>
    /// <param name="cost">TC cost of the item.</param>
    public void RecordPurchase(EntityUid userMind, string itemName, int cost)
    {
        if (!_entMan.TryGetComponent<MindComponent>(userMind, out var mindComp))
            return;

        var ev = new RoundEndAdditionalInfoCheckMapEvent(mindComp.CurrentEntity);
        _entMan.EventBus.RaiseLocalEvent(userMind, ref ev, true);

        if (ev.Cancelled)
            return;

        if (!_purchases.TryGetValue(userMind, out var data))
        {
            data = new RoundEndAntagPurchaseData
            {
                Name = mindComp.CharacterName ?? Loc.GetString("game-ticker-unknown-role"),
            };

            _purchases[userMind] = data;
        }

        data.ItemPrototypes.Add(itemName);
        data.TotalTC += cost;
    }
}

/// <summary>
/// Tracks food consumption per player during the round,
/// and provides summary info for display at round end.
/// </summary>
public sealed class FoodInfo : RoundEndInfoBase<FoodInfoState>
{
    /// <inheritdoc/>
    public override int DisplayOrder => 100;

    /// <inheritdoc/>
    public override LocId Title => Loc.GetString("additional-info-economy-categories");

    protected override void RecordInternal(ref FoodInfoState state)
    {
        state.TotalFood++;
    }

    /// <inheritdoc/>
    public override FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var (fattest, count) = GetTop(top => top.TotalFood);

        if (fattest == null ||
            !EntMan.TryGetComponent<MindComponent>(fattest.Value, out var mind) ||
            string.IsNullOrEmpty(mind.CharacterName))
            return message;

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-food-eaten",
            ("foodValue", GetTotal(top => top.TotalFood)),
            ("fattestName", mind.CharacterName),
            ("fattestValue", count)));

        return message;
    }
}

/// <summary>
/// Displays the total amount of money earned by Cargo during the round.
/// </summary>
public sealed class CargoInfo : RoundEndInfoBase<CargoInfoState>
{
    public int TotalMoneyEarned;

    public override int DisplayOrder => 102;
    public override LocId Title => Loc.GetString("additional-info-economy-categories");

    public void RecordOrder(EntityUid userMind, string itemName, int amount, int cost)
    {
        var ev = new RoundEndAdditionalInfoCheckMapEvent(userMind);
        EntMan.EventBus.RaiseLocalEvent(userMind, ref ev, true);
        if (ev.Cancelled)
            return;

        var state = Data.GetValueOrDefault(userMind);

        state.Items ??= [];

        state.Items.Add((itemName, amount, cost));
        Data[userMind] = state;
    }

    protected override void RecordInternal(ref CargoInfoState state) { }

    private (EntityUid?, int) TotalOrderPlayer()
    {
        return RoundEndInfoUtils.GetTopBy(Data, cargoData => cargoData.Items?.Count ?? 0);
    }

    private int TotalOrders()
    {
        return Data.Sum(cargoData => cargoData.Value.Items?.Count ?? 0);
    }

    private (string ItemName, int Count) GetMostOrderedItem()
    {
        var itemCounts = new Dictionary<string, int>();

        foreach (var cargoInfo in Data.Values)
        {
            if (cargoInfo.Items == null)
                continue;

            foreach (var (itemName, amount, _) in cargoInfo.Items)
            {
                if (!itemCounts.TryAdd(itemName, amount))
                    itemCounts[itemName] += amount;
            }
        }

        if (itemCounts.Count == 0)
            return (string.Empty, 0);

        var mostOrdered = itemCounts.MaxBy(pair => pair.Value);
        return (mostOrdered.Key, mostOrdered.Value);
    }

    public override FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var totalOrders = TotalOrders();
        var totalOrderPlayer = TotalOrderPlayer();
        var maxOrderedItem = GetMostOrderedItem();

        var player = totalOrderPlayer.Item1;
        var count = totalOrderPlayer.Item2;
        var name = RoundEndInfoUtils.GetMindName(EntMan, player);

        message.AddMarkupOrThrow(Loc.GetString("additional-info-cargo",
            ("totalMoney", TotalMoneyEarned),
            ("totalOrders", totalOrders),
            ("totalOrderPlayer", name),
            ("totalOrderPlayerCount", count),
            ("maxOrderedItemName", maxOrderedItem.ItemName),
            ("maxOrderedItemCount", maxOrderedItem.Count)));

        return message;
    }
}

/// <summary>
/// Displays the total research points earned during the round by RnD.
/// </summary>
public sealed class ResearchInfo : IRoundEndInfoDisplay
{
    /// <summary>
    /// Total research points accumulated.
    /// </summary>
    public int TotalPoints;

    /// <inheritdoc/>
    public int DisplayOrder => 103;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-economy-categories");

    /// <inheritdoc/>
    public FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-points-earned",
            ("totalPoints", TotalPoints)));

        return message;
    }
}

/// <summary>
/// Displays the total amount of ore mined during the round.
/// </summary>
public sealed class OreInfo : IRoundEndInfoDisplay
{
    /// <summary>
    /// Total ore units mined by players.
    /// </summary>
    public int TotalOre;

    /// <inheritdoc/>
    public int DisplayOrder => 104;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("round-end-title-ore-info");

    /// <inheritdoc/>
    public FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-ore",
            ("totalOre", TotalOre)));

        return message;
    }
}

/// <summary>
/// Tracks how many puddles each player cleaned with a mop,
/// and displays the most diligent janitor at round end.
/// </summary>
public sealed class PuddleInfo : RoundEndInfoBase<PuddleInfoState>
{
    /// <inheritdoc/>
    public override int DisplayOrder => 200;

    /// <inheritdoc/>
    public override LocId Title => Loc.GetString("additional-info-janitor-categories");

    protected override void RecordInternal(ref PuddleInfoState state)
    {
        state.TotalPuddle++;
    }

    /// <inheritdoc/>
    public override FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var (topJanitor, topValue) = GetTop(top => top.TotalPuddle);

        if (topJanitor == null ||
            !EntMan.TryGetComponent<MindComponent>(topJanitor.Value, out var mind) ||
            string.IsNullOrEmpty(mind.CharacterName))
            return message;

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-puddles",
            ("puddleValue", GetTotal(top => top.TotalPuddle)),
            ("janitorName", mind.CharacterName),
            ("janitorValue", topValue)));

        return message;
    }
}

/// <summary>
/// Tracks and displays data about firearms usage during the round,
/// including total shots fired and the player with the most shots.
/// </summary>
public sealed class GunInfo : RoundEndInfoBase<GunInfoState>
{
    /// <inheritdoc/>
    public override int DisplayOrder => 300;

    /// <inheritdoc/>
    public override LocId Title => Loc.GetString("additional-info-gun-categories");

    protected override void RecordInternal(ref GunInfoState state)
    {
        state.TotalShots++;
    }

    /// <inheritdoc/>
    public override FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var (topShooter, topValue) = GetTop(top => top.TotalShots);

        if (topShooter == null ||
            !EntMan.TryGetComponent<MindComponent>(topShooter.Value, out var mind) ||
            string.IsNullOrEmpty(mind.CharacterName))
            return message;

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-shots",
            ("ammoValue", GetTotal(total => total.TotalShots)),
            ("gunnerName", mind.CharacterName),
            ("gunnerValue", topValue)));

        return message;
    }
}

/// <summary>
/// Tracks player deaths during the round, excluding deaths on the admin test arena map.
/// Provides statistics such as total deaths, the player with the most deaths,
/// and the first recorded death for inclusion in the round end summary.
/// </summary>
public sealed class DeathInfo : RoundEndInfoBase<DeathInfoState>
{
    private readonly SharedGameTicker _gameTicker;

    public override int DisplayOrder => 400;
    public override LocId Title => Loc.GetString("additional-info-death-categories");

    public DeathInfo()
    {
        IoCManager.InjectDependencies(this);
        _gameTicker = EntMan.System<SharedGameTicker>();
    }

    public void RecordDeath(EntityUid userMind)
    {
        var ev = new RoundEndAdditionalInfoCheckMapEvent(userMind);
        EntMan.EventBus.RaiseLocalEvent(userMind, ref ev, true);

        if (ev.Cancelled)
            return;

        var state = Data.GetValueOrDefault(userMind);

        state.TimeOfDeath ??= [];
        state.TimeOfDeath.Add(_gameTicker.RoundDuration());

        Data[userMind] = state;
    }

    protected override void RecordInternal(ref DeathInfoState state) { }

    private (EntityUid?, int) GetMostDeathsByPlayer()
    {
        return RoundEndInfoUtils.GetTopBy(Data, d => d.TimeOfDeath?.Count ?? 0);
    }

    private (EntityUid?, TimeSpan) GetEarliestDeath()
    {
        EntityUid? earliestUid = null;
        var earliestTime = TimeSpan.MaxValue;

        foreach (var (uid, deathData) in Data)
        {
            if (deathData.TimeOfDeath == null)
                continue;

            foreach (var time in deathData.TimeOfDeath)
            {
                if (time >= earliestTime)
                    continue;

                earliestTime = time;
                earliestUid = uid;
            }
        }

        return (earliestUid, earliestTime);
    }

    private int TotalDeath()
    {
        return Data.Sum(d => d.Value.TimeOfDeath?.Count ?? 0);
    }

    public override FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var totalDeath = TotalDeath();
        if (totalDeath == 0)
            return message;

        var (mostDeathsUid, mostDeathsCount) = GetMostDeathsByPlayer();
        var (earliestUid, earliestTime) = GetEarliestDeath();

        if (mostDeathsUid == null || earliestUid == null)
            return message;

        var mostDeathsName = RoundEndInfoUtils.GetMindName(EntMan, mostDeathsUid.Value);
        var earliestName = RoundEndInfoUtils.GetMindName(EntMan, earliestUid.Value);

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-death",
            ("deathValue", totalDeath),
            ("suicideName", mostDeathsName),
            ("suicideValue", mostDeathsCount),
            ("suicideEarlierName", earliestName),
            ("suicideEarlierValue", earliestTime.ToString(@"hh\:mm\:ss"))));

        return message;
    }
}

public sealed class EmergencyShuttleInfo : RoundEndInfoBase<EmergencyShuttleInfoState>
{
    public override int DisplayOrder => 500;
    public override LocId Title => Loc.GetString("additional-info-emergency-shuttles-categories");
    protected override void RecordInternal(ref EmergencyShuttleInfoState state) { }

    public void RecordShuttle(EntityUid? shuttle)
    {
        if (shuttle == null)
            return;

        var state = Data.GetValueOrDefault(shuttle.Value);

        state.FirstEmergencyCallTime ??= EntMan.System<SharedGameTicker>().RoundDuration();
        state.TotalCalled++;
        state.LastEmergencyCallTime = EntMan.System<SharedGameTicker>().RoundDuration();

        Data[shuttle.Value] = state;
    }

    public override FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();
        var mostCalled = Data.FirstOrDefault(d => d.Value.FirstEmergencyCallTime != null).Value;

        if (mostCalled.FirstEmergencyCallTime == null)
            return message;

        message.AddMarkupOrThrow(Loc.GetString("additional-info-emergency-shuttles",
            ("firstShuttleTime", mostCalled.FirstEmergencyCallTime.Value.ToString(@"hh\:mm\:ss")),
            ("lastShuttleTime", mostCalled.LastEmergencyCallTime!.Value.ToString(@"hh\:mm\:ss")),
            ("countCalls", mostCalled.TotalCalled)));

        return message;
    }
}

public sealed class HealingInfo : RoundEndInfoBase<HealingInfoState>
{
    public override int DisplayOrder => 600;
    public override LocId Title => Loc.GetString("additional-info-healing-categories");

    public void RecordHealing(EntityUid healer, int amount)
    {
        var state = Data.GetValueOrDefault(healer);
        state.TotalHealed += amount;

        Data[healer] = state;
    }

    protected override void RecordInternal(ref HealingInfoState state) { }

    public override FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var total = GetTotal(total => total.TotalHealed);
        var top = GetTop(top => top.TotalHealed);
        var name = RoundEndInfoUtils.GetMindName(EntMan, top.Item1);

        message.AddMarkupOrThrow(Loc.GetString("additional-info-healing",
            ("totalHealing", total),
            ("topHealerName", name),
            ("topHealerCount", top.Item2)));

        return message;
    }
}

public sealed class StunBatonInfo : RoundEndInfoBase<StunBatonInfoState>
{
    public override int DisplayOrder => 700;
    public override LocId Title => Loc.GetString("additional-info-sec-categories");

    protected override void RecordInternal(ref StunBatonInfoState state)
    {
        state.TotalHits++;
    }

    public override FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var top = GetTop(top => top.TotalHits);
        var name = RoundEndInfoUtils.GetMindName(EntMan, top.Item1);
        message.AddMarkupOrThrow(Loc.GetString("additional-info-sec-baton",
            ("totalHits", GetTotal(total => total.TotalHits)),
            ("topSecName", name),
            ("topSecCount", top.Item2)));

        return message;
    }
}
