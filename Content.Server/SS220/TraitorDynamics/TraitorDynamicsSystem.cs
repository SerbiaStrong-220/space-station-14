// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Store.Systems;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.TraitorDynamics;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.TraitorDynamics;

/// <summary>
/// Handles the dynamic antagonist system that controls round-specific scenarios, role distribution, and economic adjustments.
/// </summary>
/// <remarks>
/// This system manages:
/// - Selection and configuration of dynamic scenarios (Dynamics) based on player count
/// - Adjustment of antagonist role limits per game rule
/// - Dynamic-specific pricing and discounts in stores
/// - Round-end reporting of active dynamic
/// </remarks>
public sealed class TraitorDynamicsSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StoreDiscountSystem _discount = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    [ValidatePrototypeId<WeightedRandomPrototype>]
    private const string WeightsProto = "WeightedDynamicsList";

    private ProtoId<DynamicPrototype>? _currentDynamic = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndAppend);
        SubscribeLocalEvent<DynamicSettedEvent>(OnDynamicAdded);
        SubscribeLocalEvent<StoreDiscountsInitializedEvent>(OnStoreFinish);
        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEnded);
        SubscribeLocalEvent<DynamicRemoveEvent>(OnDynamicRemove);
    }

    private void OnStoreFinish(ref StoreDiscountsInitializedEvent ev)
    {
        if (_currentDynamic == null)
            return;

        ApplyDynamicPrice(ev.Store, ev.Listings, _currentDynamic.Value);
    }

    private void OnDynamicAdded(DynamicSettedEvent ev)
{
    var dynamic = _prototype.Index(ev.Dynamic);
    var rules = _gameTicker.GetAllGameRulePrototypes();

    var roleMap = new Dictionary<string, List<(EntityPrototype Rule, AntagSelectionComponent Comp)>>();

    foreach (var rule in rules)
    {
        if (!rule.TryGetComponent<AntagSelectionComponent>(out var selection, EntityManager.ComponentFactory))
            continue;

        foreach (var def in selection.Definitions)
        {
            var allRoles = def.PrefRoles.Select(p => p.Id);

            foreach (var role in allRoles)
            {
                if (!roleMap.TryGetValue(role, out var list))
                {
                    list = new List<(EntityPrototype, AntagSelectionComponent)>();
                    roleMap[role] = list;
                }

                list.Add((rule, selection));
            }
        }
    }

    foreach (var (roleProto, limit) in dynamic.AntagLimits)
    {
        var roleId = roleProto.Id;

        if (!roleMap.TryGetValue(roleId, out var entries))
            continue;

        foreach (var (_, comp) in entries)
        {
            _antag.SetAntagLimit(comp, roleId, newMax: limit);
        }
    }

    var query = EntityQueryEnumerator<StoreComponent>();
    while (query.MoveNext(out var store, out var comp))
    {
        if (!comp.UseDynamicPrices)
            continue;

        if (comp.AccountOwner == null)
            continue;

        var listings = _store.GetAvailableListings(comp.AccountOwner.Value, store, comp).ToArray();
        ApplyDynamicPrice(store, listings, dynamic.ID);
    }
}

    private void OnRoundEndAppend(RoundEndTextAppendEvent ev)
    {
        var dynamic = GetCurrentDynamic();

        if (!_prototype.TryIndex(dynamic, out var dynamicProto))
            return;

        var locName = Loc.GetString(dynamicProto.Name);
        ev.AddLine(Loc.GetString("dynamic-show-end-round", ("dynamic", locName)));
    }

    private void OnRoundEnded(RoundEndSystemChangedEvent ev)
    {
        if (!_currentDynamic.HasValue)
            return;

        RemoveDynamic();
    }

    private void OnDynamicRemove(DynamicRemoveEvent ev)
    {
        _currentDynamic = null;
        ResetDynamicPrices();
    }

    private void ApplyDynamicPrice(EntityUid store, IReadOnlyList<ListingDataWithCostModifiers> listings, ProtoId<DynamicPrototype> currentDynamic)
    {
        var itemDiscounts = _discount.GetItemsDiscount(store, listings);
        var discountsLookup = itemDiscounts.ToDictionary(d => d.ListingId, d => d);

        foreach (var listing in listings)
        {
            if (!listing.DynamicsPrices.TryGetValue(currentDynamic, out var dynamicPrice))
                continue;

            listing.SetNewCost(dynamicPrice);

            if (!listing.DiscountCategory.HasValue)
                continue;

            if (!discountsLookup.TryGetValue(listing.ID, out var itemDiscount))
                continue;

            listing.RemoveCostModifier(listing.DiscountCategory.Value);
            var finalPrices = ApplyDiscountsToPrice(dynamicPrice, listing, itemDiscount);

            listing.SetExactPrice(listing.DiscountCategory.Value, finalPrices);
        }
    }

    private Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> ApplyDiscountsToPrice(
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> basePrice,
        ListingDataWithCostModifiers listing,
        ItemDiscounts itemDiscounts)
    {
        var finalPrice = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(basePrice);
        foreach (var (currency, amount) in basePrice)
        {
            finalPrice[currency] = amount;
        }

        foreach (var discount in itemDiscounts.Discounts)
        {
            if (!listing.OriginalCost.ContainsKey(discount.Key))
                continue;

            if (!finalPrice.TryGetValue(discount.Key, out var currentPrice))
                continue;

            var rawValue = currentPrice * discount.Value;
            var roundedValue = Math.Round(rawValue.Double(), MidpointRounding.AwayFromZero);
            finalPrice[discount.Key] = Math.Max(currentPrice.Double() - roundedValue, 1);
        }

        return new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(finalPrice);
    }

    /// <summary>
    /// Gets and sets a random dynamic based on the current number of ready players.
    /// </summary>
    public void SetRandomDynamic()
    {
        var countPlayers = _gameTicker.ReadyPlayerCount();
        var dynamic = GetRandomDynamic(countPlayers);
        SetDynamic(dynamic);
    }

    /// <summary>
    /// Sets the specified dynamic of DynamicPrototype
    /// </summary>
    /// <param name="proto"> The prototype ID of the dynamic mode </param>
    public void SetDynamic(DynamicPrototype dynamicProto)
    {
        var attemptEv = new DynamicSetAttempt(dynamicProto.ID);
        RaiseLocalEvent(attemptEv);

        if (attemptEv.Cancelled)
            return;

        _currentDynamic = dynamicProto;
        _admin.Add(LogType.EventStarted, LogImpact.High, $"Dynamic {dynamicProto.ID} was setted");

        _chatManager.SendAdminAnnouncement(Loc.GetString("dynamic-was-set", ("dynamic", dynamicProto.ID)));

        var ev = new DynamicSettedEvent(dynamicProto.ID);
        RaiseLocalEvent(ev);

        if (dynamicProto.LoreNames == default || !_prototype.TryIndex(dynamicProto.LoreNames, out var namesProto))
            return;

        dynamicProto.SelectedLoreName = _random.Pick(namesProto.ListNames);
    }

    public void SetDynamic(string proto)
    {
        if (!_prototype.TryIndex<DynamicPrototype>(proto, out var dynamicProto, true))
            return;

        SetDynamic(dynamicProto);
    }

    public void RemoveDynamic()
    {
        var ev = new DynamicRemoveEvent();
        RaiseLocalEvent(ev);
    }

    /// <summary>
    /// Gets a random DynamicPrototype from WeightedRandomPrototype, weeding out unsuitable dynamics
    /// </summary>
    /// <param name="playerCount"> current number of ready players, by this indicator the required number is compared </param>
    /// <param name="force"> ignore player checks and force any dynamics </param>
    /// <returns></returns>
    public string GetRandomDynamic(int playerCount = 0, bool force = false)
    {
        var validWeight = _prototype.Index<WeightedRandomPrototype>(WeightsProto);
        var selectedDynamic = string.Empty;

        var originalProto = new Dictionary<string, float>(validWeight.Weights);

        try
        {
            while (validWeight.Weights.Keys.Count > 0)
            {
                var currentDynamic = validWeight.Pick(_random);

                if (!_prototype.TryIndex<DynamicPrototype>(currentDynamic, out var dynamicProto))
                {
                    validWeight.Weights.Remove(currentDynamic);
                    continue;
                }

                if (playerCount == 0 || force)
                {
                    selectedDynamic = dynamicProto.ID;
                    break;
                }

                if (playerCount >= dynamicProto.PlayersRequerment)
                {
                    selectedDynamic = dynamicProto.ID;
                    break;
                }

                validWeight.Weights.Remove(currentDynamic);
            }
        }

        finally
        {
            validWeight.Weights.Clear();
            foreach (var k in originalProto)
            {
                validWeight.Weights[k.Key] = k.Value;
            }
        }

        return selectedDynamic;
    }

    private void ResetDynamicPrices()
    {
        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var store, out var comp))
        {
            if (!comp.UseDynamicPrices)
                continue;

            if (comp.AccountOwner == null)
                continue;

            var listings = _store.GetAvailableListings(comp.AccountOwner.Value, store, comp).ToArray();
            foreach (var listing in listings)
            {
                listing.ReturnCostFromCatalog();
            }
        }
    }


    /// <summary>
    /// Tries to find the type of dynamic while in Traitor game rule
    /// </summary>
    /// <returns>installed dynamic</returns>
    public ProtoId<DynamicPrototype>? GetCurrentDynamic()
    {
        return _currentDynamic;
    }

    public sealed class DynamicSettedEvent : EntityEventArgs
    {
        public ProtoId<DynamicPrototype> Dynamic;

        public DynamicSettedEvent(ProtoId<DynamicPrototype> dynamic)
        {
            Dynamic = dynamic;
        }
    }

    public sealed class DynamicSetAttempt : CancellableEntityEventArgs
    {
        public ProtoId<DynamicPrototype> Dynamic;

        public DynamicSetAttempt(ProtoId<DynamicPrototype> dynamic)
        {
            Dynamic = dynamic;
        }
    }

    public sealed class DynamicRemoveEvent : EntityEventArgs
    {
    }
}
