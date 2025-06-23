using System.Linq;
using Content.Server.Administration.Systems;
using Content.Server.Mind;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.SS220.AdditionalInfoForRoundEnd;
using Robust.Shared.Utility;

namespace Content.Server.SS220.AdditionalInfoForRoundEnd;

/// <summary>
/// Manages all IRoundEndInfo data used for compiling round-end summaries.
/// Responsible for creating, storing, retrieving, and clearing instances of various info providers.
/// </summary>
public sealed class RoundEndInfoManager
{
    private readonly Dictionary<Type, IRoundEndInfo> _infos = new();

    /// <summary>
    /// Ensures that an instance of the specified IRoundEndInfo type exists and returns it.
    /// If none exists, a new one is created, initialized if applicable, and stored.
    /// </summary>
    /// <typeparam name="T">The type implementing IRoundEndInfo.</typeparam>
    /// <returns>An instance of the requested IRoundEndInfo type.</returns>
    public T EnsureInfo<T>() where T : class, IRoundEndInfo, new()
    {
        if (_infos.TryGetValue(typeof(T), out var existing))
            return (T) existing;

        var instance = new T();

        if (instance is IRoundEndInfoWithInit init)
            init.Initialize();

        _infos.Add(typeof(T), instance);
        return instance;
    }

    /// <summary>
    /// Clears all stored IRoundEndInfo instances from the manager.
    /// </summary>
    public void ClearAllDatas()
    {
        _infos.Clear();
    }

    /// <summary>
    /// Returns an enumeration of all currently stored IRoundEndInfo instances.
    /// </summary>
    public IEnumerable<IRoundEndInfo> GetAllInfos() => _infos.Values;
}

/// <summary>
/// Base interface for any data provider that contributes to round-end summaries.
/// </summary>
public interface IRoundEndInfo
{
}

/// <summary>
/// Extension interface for IRoundEndInfo types that require initialization before use.
/// </summary>
public interface IRoundEndInfoWithInit : IRoundEndInfo
{
    /// <summary>
    /// Called once when the info is created to set up dependencies or internal state.
    /// </summary>
    void Initialize();
}

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
    /// <param name="message">A formatted message object to append data to.</param>
    void AddSummaryText(FormattedMessage message);
}

/// <summary>
/// Stores and aggregates antagonist item purchases during the round.
/// Each purchase is tracked per player and contributes to the round-end summary.
/// </summary>
public sealed class AntagPurchaseInfo : IRoundEndInfoWithInit
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private MindSystem _mind = default!;

    private readonly Dictionary<EntityUid, RoundEndAntagPurchaseData> _purchases = new();

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _mind = _entMan.System<MindSystem>();
    }

    /// <summary>
    /// Records a purchased antagonist item for the given user.
    /// </summary>
    /// <param name="user">The entity that made the purchase.</param>
    /// <param name="itemName">The prototype ID of the purchased item.</param>
    /// <param name="cost">TC cost of the item.</param>
    public void AddPurchase(EntityUid user, string itemName, int cost)
    {
        if (!_mind.TryGetMind(user, out var mindId, out var comp))
            return;

        if (!_purchases.TryGetValue(mindId, out var data))
        {
            data = new RoundEndAntagPurchaseData
            {
                Name = comp.CharacterName ?? Loc.GetString("game-ticker-unknown-role")
            };

            _purchases[mindId] = data;
        }

        data.ItemPrototypes.Add(itemName);
        data.TotalTC += cost;
    }

    /// <summary>
    /// Retrieves all recorded antagonist purchases, grouped by player name.
    /// </summary>
    /// <returns>A dictionary of player names to their purchase data.</returns>
    public Dictionary<string, RoundEndAntagPurchaseData> GetAllPurchases()
    {
        var result = new Dictionary<string, RoundEndAntagPurchaseData>();

        foreach (var (_, data) in _purchases)
        {
            result[data.Name] = data;
        }

        return result;
    }
}

/// <summary>
/// Tracks food consumption per player during the round,
/// and provides summary info for display at round end.
/// </summary>
public sealed class FoodInfo : IRoundEndInfoWithInit, IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private MindSystem _mind = default!;

    private readonly Dictionary<EntityUid, FoodData> _foodPlayersData = new();

    /// <inheritdoc/>
    public int DisplayOrder => 100;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-economy-categories");

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _mind = _entMan.System<MindSystem>();
    }

    /// <summary>
    /// Records a food-related event for the specified player's mind.
    /// </summary>
    /// <param name="user">The entity associated with the eating action.</param>
    public void AddValueForMind(EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mind, out _))
            return;

        if (!_foodPlayersData.TryGetValue(mind, out var foodData))
        {
            foodData = new FoodData();
            _foodPlayersData[mind] = foodData;
        }

        foodData.AmountFood++;
    }

    /// <summary>
    /// Returns the player who consumed the most food during the round.
    /// </summary>
    private (string, int) TotalFattestPlayer()
    {
        var total = (string.Empty, 0);
        var current = 0;

        foreach (var (uid, foodData) in _foodPlayersData)
        {
            if (foodData.AmountFood <= current)
                continue;

            current = foodData.AmountFood;

            if (_entMan.TryGetComponent(uid, out MindComponent? comp))
            {
                total = (comp.CharacterName ?? Loc.GetString("game-ticker-unknown-role"), current);
            }
        }

        return total;
    }

    /// <summary>
    /// Calculates the total amount of food consumed by all tracked players.
    /// </summary>
    private int TotalFoodEaten()
    {
        return _foodPlayersData.Sum(foodData => foodData.Value.AmountFood);
    }

    /// <inheritdoc/>
    public void AddSummaryText(FormattedMessage message)
    {
        var (fattest, count) = TotalFattestPlayer();
        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-food-eaten",
            ("foodValue", TotalFoodEaten()),
            ("fattestName", fattest),
            ("fattestValue", count)));
    }
}

/// <summary>
/// Displays the total amount of money earned by Cargo during the round.
/// </summary>
public sealed class CargoInfo : IRoundEndInfoDisplay
{
    /// <summary>
    /// Total station credits earned through cargo operations.
    /// </summary>
    public int TotalMoneyEarned;

    /// <inheritdoc/>
    public int DisplayOrder => 102;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-economy-categories");

    /// <inheritdoc/>
    public void AddSummaryText(FormattedMessage message)
    {
        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-money-earned",
            ("totalMoney", TotalMoneyEarned)));
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
    public void AddSummaryText(FormattedMessage message)
    {
        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-points-earned",
            ("totalPoints", TotalPoints)));
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
    public void AddSummaryText(FormattedMessage message)
    {
        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-ore",
            ("totalOre", TotalOre)));
    }
}

/// <summary>
/// Tracks how many puddles each player cleaned with a mop,
/// and displays the most diligent janitor at round end.
/// </summary>
public sealed class PuddleInfo : IRoundEndInfoWithInit, IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private MindSystem _mind = default!;

    private readonly Dictionary<EntityUid, PuddleData> _puddleDatas = new();

    /// <inheritdoc/>
    public int DisplayOrder => 200;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-janitor-categories");

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _mind = _entMan.System<MindSystem>();
    }

    /// <summary>
    /// Increments the puddle-cleaning count for a player who used a mop.
    /// </summary>
    /// <param name="user">The entity that cleaned a puddle.</param>
    public void AddValueForMind(EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mind, out _))
            return;

        if (!_puddleDatas.TryGetValue(mind, out var puddleData))
        {
            puddleData = new PuddleData();
            _puddleDatas[mind] = puddleData;
        }

        puddleData.TotalPuddle++;
    }

    /// <summary>
    /// Finds the player who mopped the most puddles.
    /// </summary>
    private (string, int) TotalPuddleByPlayer()
    {
        var total = (string.Empty, 0);

        var current = 0;

        foreach (var (uid, puddleData) in _puddleDatas)
        {
            if (puddleData.TotalPuddle <= current)
                continue;

            current = puddleData.TotalPuddle;

            if (_entMan.TryGetComponent(uid, out MindComponent? comp))
            {
                total = (comp.CharacterName ?? Loc.GetString("game-ticker-unknown-role"), current);
            }
        }

        return total;
    }

    /// <summary>
    /// Returns the total number of puddles cleaned by all players.
    /// </summary>
    private int TotalPuddles()
    {
        return _puddleDatas.Sum(puddleData => puddleData.Value.TotalPuddle);
    }

    /// <inheritdoc/>
    public void AddSummaryText(FormattedMessage message)
    {
        var (topName, topValue) = TotalPuddleByPlayer();
        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-puddles",
            ("puddleValue", TotalPuddles()),
            ("janitorName", topName),
            ("janitorValue", topValue)));
    }
}

/// <summary>
/// Tracks and displays data about firearms usage during the round,
/// including total shots fired and the player with the most shots.
/// </summary>
public sealed class GunInfo : IRoundEndInfoWithInit, IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private MindSystem _mind = default!;

    private readonly Dictionary<EntityUid, GunData> _gunDatas = new();

    /// <inheritdoc/>
    public int DisplayOrder => 300;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-gun-categories");

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _mind = _entMan.System<MindSystem>();
    }

    /// <summary>
    /// Records that a shot was fired by the specified player.
    /// </summary>
    public void AddValueForMind(EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mind, out _))
            return;

        if (!_gunDatas.TryGetValue(mind, out var gunData))
        {
            gunData = new GunData();
            _gunDatas[mind] = gunData;
        }

        gunData.TotalShots++;
    }

    /// <summary>
    /// Identifies the player who fired the most shots.
    /// </summary>
    private (string, int) TotalShotsByPlayer()
    {
        var total = (string.Empty, 0);

        var current = 0;

        foreach (var (uid, gunData) in _gunDatas)
        {
            if (gunData.TotalShots <= current)
                continue;

            current = gunData.TotalShots;

            if (_entMan.TryGetComponent(uid, out MindComponent? comp))
            {
                total = (comp.CharacterName ?? Loc.GetString("game-ticker-unknown-role"), current);
            }
        }

        return total;
    }

    /// <summary>
    /// Calculates the total number of shots fired during the round.
    /// </summary>
    private int TotalShots()
    {
        return _gunDatas.Sum(gunData => gunData.Value.TotalShots);
    }

    /// <inheritdoc/>
    public void AddSummaryText(FormattedMessage message)
    {
        var (topName, topValue) = TotalShotsByPlayer();
        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-shots",
            ("ammoValue", TotalShots()),
            ("gunnerName", topName),
            ("gunnerValue", topValue)));
    }
}

/// <summary>
/// Tracks player deaths during the round, excluding deaths on the admin test arena map.
/// Provides statistics such as total deaths, the player with the most deaths,
/// and the first recorded death for inclusion in the round end summary.
/// </summary>
public sealed class DeathInfo : IRoundEndInfoWithInit, IRoundEndInfoDisplay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private MindSystem _mind = default!;
    private SharedGameTicker _gameTicker = default!;
    private AdminTestArenaSystem _adminTestArena = default!;
    private SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, DeathData> _deathDatas = new();

    /// <inheritdoc/>
    public int DisplayOrder => 400;

    /// <inheritdoc/>
    public LocId Title => Loc.GetString("additional-info-death-categories");

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _gameTicker = _entMan.System<SharedGameTicker>();
        _mind = _entMan.System<MindSystem>();
        _adminTestArena = _entMan.System<AdminTestArenaSystem>();
        _transform = _entMan.System<SharedTransformSystem>();
    }

    /// <summary>
    /// Records the time of death for the given player if valid and not in the admin arena.
    /// </summary>
    public void AddValueForMind(EntityUid user)
    {
        var map = _transform.GetMap(user);

        if (map != null && _adminTestArena.ArenaMap.ContainsValue(map.Value))
            return;

        if (!_mind.TryGetMind(user, out var mind, out _))
            return;

        if (!_deathDatas.TryGetValue(mind, out var deathData))
        {
            deathData = new DeathData();
            _deathDatas[mind] = deathData;
        }

        deathData.TimeOfDeath.Add(_gameTicker.RoundDuration());
    }

    /// <summary>
    /// Finds the player with the highest number of recorded deaths.
    /// </summary>
    private (string name, int deathCount) GetMostDeathsByPlayer()
    {
        var mostDeathsName = string.Empty;
        var mostDeathsCount = 0;

        foreach (var (uid, deathData) in _deathDatas)
        {
            if (!_entMan.TryGetComponent(uid, out MindComponent? mind))
                continue;

            var name = mind.CharacterName ?? Loc.GetString("game-ticker-unknown-role");
            var count = deathData.TimeOfDeath.Count;

            if (count <= mostDeathsCount)
                continue;

            mostDeathsCount = count;
            mostDeathsName = name;
        }

        return (mostDeathsName, mostDeathsCount);
    }

    /// <summary>
    /// Identifies the first death of the round and when it occurred.
    /// </summary>
    private (string name, TimeSpan time) GetEarliestDeath()
    {
        var earliestName = string.Empty;
        var earliestTime = TimeSpan.Zero;

        foreach (var (uid, deathData) in _deathDatas)
        {
            foreach (var time in deathData.TimeOfDeath)
            {
                if (time <= earliestTime)
                    continue;

                earliestTime = time;

                if (_entMan.TryGetComponent(uid, out MindComponent? mind))
                    earliestName = mind.CharacterName ?? Loc.GetString("game-ticker-unknown-role");
                else
                    earliestName = Loc.GetString("game-ticker-unknown-role");
            }
        }

        return (earliestName, earliestTime);
    }

    /// <summary>
    /// Calculates the total number of deaths recorded this round.
    /// </summary>
    private int TotalDeath()
    {
        return _deathDatas.Sum(deathData => deathData.Value.TimeOfDeath.Count);
    }

    /// <inheritdoc/>
    public void AddSummaryText(FormattedMessage message)
    {
        var totalDeath = TotalDeath();

        if (totalDeath == 0)
            return;

        var (suicideName, suicideValue) = GetMostDeathsByPlayer();
        var (suicideEarlierName, suicideEarlierValue) = GetEarliestDeath();
        message.AddMarkupOrThrow(Loc.GetString("additional-info-total-death",
            ("deathValue", totalDeath),
            ("suicideName", suicideName),
            ("suicideValue", suicideValue),
            ("suicideEarlierName", suicideEarlierName),
            ("suicideEarlierValue", suicideEarlierValue.ToString(@"hh\:mm\:ss"))));
    }
}
