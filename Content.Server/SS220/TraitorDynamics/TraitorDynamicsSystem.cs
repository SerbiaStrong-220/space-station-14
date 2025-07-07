// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Store.Systems;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.TraitorDynamics;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
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
public sealed class TraitorDynamicsSystem : SharedTraitorDynamicsSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StoreDiscountSystem _discount = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    [ValidatePrototypeId<DiscountCategoryPrototype>]
    private const string Discount = "usualDiscounts";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndAppend);
        SubscribeLocalEvent<DynamicAddedEvent>(OnDynamicAdded);
        SubscribeLocalEvent<StoreFinishedEvent>(OnStoreInit);
    }

    private void OnStoreInit(ref StoreFinishedEvent ev)
    {
        if (CurrentDynamic == null)
            return;

        ApplyDynamicPrice(ev.Store, ev.Listings, CurrentDynamic.Value);
    }

    private void OnDynamicAdded(DynamicAddedEvent ev)
    {
        var dynamic = _prototype.Index(ev.Dynamic);
        var rules = _gameTicker.GetAllGameRulePrototypes();

        foreach (var rule in rules)
        {
            if (!rule.TryGetComponent<AntagSelectionComponent>(out var selection, EntityManager.ComponentFactory))
                continue;

            if (!dynamic.AntagLimits.TryGetValue(rule.ID, out var value))
                continue;

            _antag.SetMaxAntags(selection, value);
        }

        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var store, out var comp))
        {
            if (!comp.UseDynamicPrices)
                continue;

            if (comp.AccountOwner == null)
                return;

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

    private void ApplyDynamicPrice(EntityUid store, IReadOnlyList<ListingDataWithCostModifiers> listings, ProtoId<DynamicPrototype> currentDynamic)
    {
        var itemDiscounts = _discount.GetItemsDiscount(store, listings);

        foreach (var listing in listings)
        {
            if (!listing.DynamicsPrices.TryGetValue(currentDynamic, out var dynamicPrice))
                continue;

            listing.RemoveCostModifier(Discount);
            listing.SetNewCost(dynamicPrice);

            var finalPrice = ApplyDiscountsToPrice(dynamicPrice, listing, itemDiscounts);
            listing.SetExactPrice(Discount, finalPrice);
        }
    }

    private Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> ApplyDiscountsToPrice(
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> basePrice,
        ListingDataWithCostModifiers listing,
        Dictionary<string, Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>> itemDiscounts)
    {
        if (!itemDiscounts.TryGetValue(listing.ID, out var currencyDiscounts))
            return basePrice;

        var finalPrice = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(basePrice);

        foreach (var (currency, discountPercent) in currencyDiscounts)
        {
            if (!listing.OriginalCost.ContainsKey(currency))
                continue;

            if (!finalPrice.TryGetValue(currency, out var currentPrice))
                continue;

            var rawValue = currentPrice * discountPercent;
            var roundedValue = Math.Round(rawValue.Double(), MidpointRounding.AwayFromZero);
            finalPrice[currency] = Math.Max(currentPrice.Double() - roundedValue, 1);
        }

        return finalPrice;
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
    public void SetDynamic(string proto)
    {
        if (!_prototype.TryIndex<DynamicPrototype>(proto, out var dynamicProto, true))
            return;

        var attemptEv = new DynamicSetAttempt(dynamicProto.ID);
        RaiseLocalEvent(attemptEv);

        if (attemptEv.Cancelled)
            return;

        CurrentDynamic = dynamicProto;
        _admin.Add(LogType.AntagSelection, LogImpact.High, $"Dynamic {dynamicProto.ID} was setted"); // TODO: log type must be changed

        _chatManager.SendAdminAnnouncement(Loc.GetString("dynamic-was-set", ("dynamic", dynamicProto.ID)));

        var ev = new DynamicAddedEvent(dynamicProto.ID);
        RaiseLocalEvent(ev);

        if (dynamicProto.LoreNames == default || !_prototype.TryIndex(dynamicProto.LoreNames, out var namesProto))
            return;

        dynamicProto.SelectedLoreName = _random.Pick(namesProto.ListNames);
    }
}
