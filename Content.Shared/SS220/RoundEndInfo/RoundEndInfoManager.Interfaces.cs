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
    /// <param name="userMind">The mind entity, that made the purchase.</param>
    /// <param name="itemName">The prototype ID of the purchased item.</param>
    /// <param name="cost">TC cost of the item.</param>
    public void RecordPurchase(EntityUid userMind, string itemName, int cost)
    {
        if (!_entMan.TryGetComponent<MindComponent>(userMind, out var mindComp))
            return;

        if (!_purchases.TryGetValue(userMind, out var data))
        {
            data = new RoundEndAntagPurchaseData
            {
                Name = mindComp.CharacterName ?? Loc.GetString("game-ticker-unknown-role")
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
public sealed class FoodInfo : IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private readonly Dictionary<EntityUid, FoodInfoState> _foodPlayersData = new();

    /// <inheritdoc/>
    public int DisplayOrder => 100;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-economy-categories");

    /// <summary>
    /// Records a food-related event for the specified player's mind.
    /// </summary>
    /// <param name="userMind">The mind entity associated with the eating action.</param>
    public void RecordConsumption(EntityUid userMind)
    {
        if (!_foodPlayersData.TryGetValue(userMind, out var foodInfoState))
        {
            foodInfoState = new FoodInfoState();
            _foodPlayersData[userMind] = foodInfoState;
        }

        foodInfoState.TotalFood++;
        _foodPlayersData[userMind] = foodInfoState;
    }

    /// <summary>
    /// Returns the player who consumed the most food during the round.
    /// </summary>
    private (EntityUid?, int) TotalFattestPlayer()
    {
        return RoundEndInfoUtils.GetTopBy(_foodPlayersData, d => d.TotalFood);
    }

    /// <summary>
    /// Calculates the total amount of food consumed by all tracked players.
    /// </summary>
    private int TotalFoodEaten()
    {
        return _foodPlayersData.Sum(foodInfoState => foodInfoState.Value.TotalFood);
    }

    /// <inheritdoc/>
    public FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var (fattest, count) = TotalFattestPlayer();

        if (fattest == null
            || !_entMan.TryGetComponent<MindComponent>(fattest.Value, out var mind)
            || string.IsNullOrEmpty(mind.CharacterName))
            return message;

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-food-eaten",
            ("foodValue", TotalFoodEaten()),
            ("fattestName", mind.CharacterName),
            ("fattestValue", count)));

        return message;
    }
}

/// <summary>
/// Displays the total amount of money earned by Cargo during the round.
/// </summary>
public sealed class CargoInfo : IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private readonly Dictionary<EntityUid, CargoInfoState> _cargoData = new();

    /// <summary>
    /// Total station credits earned through cargo operations.
    /// </summary>
    public int TotalMoneyEarned;

    /// <inheritdoc/>
    public int DisplayOrder => 102;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-economy-categories");

    public void RecordOrder(EntityUid userMind, string itemName, int amount, int cost)
    {
        if (!_cargoData.TryGetValue(userMind, out var cargoInfoState))
        {
            cargoInfoState = new CargoInfoState();
            _cargoData[userMind] = cargoInfoState;
        }

        cargoInfoState.Items.Add((itemName, amount, cost));
    }

    private (EntityUid?, int) TotalOrderPlayer()
    {
        return RoundEndInfoUtils.GetTopBy(_cargoData, cargoData => cargoData.Items.Count);
    }

    private int TotalOrders()
    {
        return _cargoData.Sum(cargoData => cargoData.Value.Items.Count);
    }

    private (string ItemName, int Count) GetMostOrderedItem()
    {
        var itemCounts = new Dictionary<string, int>();

        foreach (var cargoInfo in _cargoData.Values)
        {
            foreach (var (itemName, amount, _) in cargoInfo.Items)
            {
                if (!itemCounts.TryAdd(itemName, amount))
                    itemCounts[itemName] += amount;
            }
        }

        if (itemCounts.Count == 0)
            return (string.Empty, 0);

        var mostOrdered = itemCounts
            .OrderByDescending(pair => pair.Value)
            .First();

        return (mostOrdered.Key, mostOrdered.Value);
    }

    /// <inheritdoc/>
    public FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var totalOrders = TotalOrders();
        var totalOrderPlayer = TotalOrderPlayer();
        var maxOrderedItem = GetMostOrderedItem();

        var player = totalOrderPlayer.Item1;
        var count = totalOrderPlayer.Item2;

        var name = RoundEndInfoUtils.GetMindName(_entMan, player);

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
public sealed class PuddleInfo : IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private readonly Dictionary<EntityUid, PuddleInfoState> _puddleData = new();

    /// <inheritdoc/>
    public int DisplayOrder => 200;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-janitor-categories");

    /// <summary>
    /// Increments the puddle-cleaning count for a player who used a mop.
    /// </summary>
    /// <param name="userMind">The mind entity that cleaned a puddle.</param>
    public void RecordCleaning(EntityUid userMind)
    {
        if (!_puddleData.TryGetValue(userMind, out var puddleInfoState))
        {
            puddleInfoState = new PuddleInfoState();
            _puddleData[userMind] = puddleInfoState;
        }

        puddleInfoState.TotalPuddle++;
        _puddleData[userMind] = puddleInfoState;
    }

    /// <summary>
    /// Finds the player who mopped the most puddles.
    /// </summary>
    private (EntityUid?, int) TotalPuddleByPlayer()
    {
        return RoundEndInfoUtils.GetTopBy(_puddleData, d => d.TotalPuddle);
    }

    /// <summary>
    /// Returns the total number of puddles cleaned by all players.
    /// </summary>
    private int TotalPuddles()
    {
        return _puddleData.Sum(puddleData => puddleData.Value.TotalPuddle);
    }

    /// <inheritdoc/>
    public FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var (topJanitor, topValue) = TotalPuddleByPlayer();

        if (topJanitor == null
            || !_entMan.TryGetComponent<MindComponent>(topJanitor.Value, out var mind)
            || string.IsNullOrEmpty(mind.CharacterName))
            return message;

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-puddles",
            ("puddleValue", TotalPuddles()),
            ("janitorName", mind.CharacterName),
            ("janitorValue", topValue)));

        return message;
    }
}

/// <summary>
/// Tracks and displays data about firearms usage during the round,
/// including total shots fired and the player with the most shots.
/// </summary>
public sealed class GunInfo : IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private readonly Dictionary<EntityUid, GunInfoState> _gunData = new();

    /// <inheritdoc/>
    public int DisplayOrder => 300;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-gun-categories");

    /// <summary>
    /// Records that a shot was fired by the specified player.
    /// </summary>
    public void RecordShoot(EntityUid userMind)
    {
        if (!_gunData.TryGetValue(userMind, out var gunInfoState))
        {
            gunInfoState = new GunInfoState();
            _gunData[userMind] = gunInfoState;
        }

        gunInfoState.TotalShots++;
        _gunData[userMind] = gunInfoState;
    }

    /// <summary>
    /// Identifies the player who fired the most shots.
    /// </summary>
    private (EntityUid?, int) TotalShotsByPlayer()
    {
        return RoundEndInfoUtils.GetTopBy(_gunData, d => d.TotalShots);
    }

    /// <summary>
    /// Calculates the total number of shots fired during the round.
    /// </summary>
    private int TotalShots()
    {
        return _gunData.Sum(gunData => gunData.Value.TotalShots);
    }

    /// <inheritdoc/>
    public FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var (topShooter, topValue) = TotalShotsByPlayer();

        if (topShooter == null
            || !_entMan.TryGetComponent<MindComponent>(topShooter.Value, out var mind)
            || string.IsNullOrEmpty(mind.CharacterName))
            return message;

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-shots",
            ("ammoValue", TotalShots()),
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
public sealed class DeathInfo : IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private readonly SharedGameTicker _gameTicker;

    private readonly Dictionary<EntityUid, DeathInfoState> _deathData = new();

    /// <inheritdoc/>
    public int DisplayOrder => 400;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-death-categories");

    public DeathInfo()
    {
        IoCManager.InjectDependencies(this);
        _gameTicker = _entMan.System<SharedGameTicker>();
    }

    /// <summary>
    /// Records the time of death for the given player if valid and not in the admin arena.
    /// </summary>
    public void RecordDeath(EntityUid userMind)
    {
        if (!_deathData.TryGetValue(userMind, out var deathInfoState))
        {
            deathInfoState = new DeathInfoState();
            _deathData[userMind] = deathInfoState;
        }

        deathInfoState.TimeOfDeath.Add(_gameTicker.RoundDuration());
    }

    /// <summary>
    /// Finds the player with the highest number of recorded deaths.
    /// </summary>
    private (EntityUid?, int) GetMostDeathsByPlayer()
    {
        return RoundEndInfoUtils.GetTopBy(_deathData, d => d.TimeOfDeath.Count);
    }

    /// <summary>
    /// Identifies the first death of the round and when it occurred.
    /// </summary>
    private (EntityUid? uid, TimeSpan time) GetEarliestDeath()
    {
        EntityUid? earliestUid = null;
        var earliestTime = TimeSpan.MaxValue;

        foreach (var (uid, deathData) in _deathData)
        {
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

    /// <summary>
    /// Calculates the total number of deaths recorded this round.
    /// </summary>
    private int TotalDeath()
    {
        return _deathData.Sum(deathData => deathData.Value.TimeOfDeath.Count);
    }

    /// <inheritdoc/>
    public FormattedMessage GetSummaryText()
    {
        var message = new FormattedMessage();

        var totalDeath = TotalDeath();

        if (totalDeath == 0)
            return message;

        var (suicideEntity, suicideValue) = GetMostDeathsByPlayer();
        var (suicideEarlierEntity, suicideEarlierValue) = GetEarliestDeath();

        if (suicideEntity == null
            || suicideEarlierEntity == null)
            return message;

        var suicideName = RoundEndInfoUtils.GetMindName(_entMan, suicideEntity.Value);
        var suicideEarlierName = RoundEndInfoUtils.GetMindName(_entMan, suicideEarlierEntity.Value);

        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-death",
            ("deathValue", totalDeath),
            ("suicideName", suicideName),
            ("suicideValue", suicideValue),
            ("suicideEarlierName", suicideEarlierName),
            ("suicideEarlierValue", suicideEarlierValue.ToString(@"hh\:mm\:ss"))));

        return message;
    }
}
