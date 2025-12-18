// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.CrewManifest;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Server.StoreDiscount.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.TraitorDynamics;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Prometheus;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

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
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CrewManifestSystem _crewManifest = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StoreDiscountSystem _discount = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    private static Counter _chosenDynamicsModes = default!;

    private readonly ProtoId<WeightedRandomPrototype> _weightsProto = "WeightedDynamicsList";

    private ProtoId<DynamicPrototype>? _currentDynamic = null;

    public override void Initialize()
    {
        base.Initialize();

        _chosenDynamicsModes = Metrics.CreateCounter(
            "dynamic_traitors_modes",
            "Shows what mode of dynamic traitor was choosen or forced by admins",
            new CounterConfiguration()
            {
                LabelNames = ["dynamicPrototypeId"]
            });

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
        _chosenDynamicsModes.WithLabels([ev.Dynamic]).Inc();

        var dynamic = _prototype.Index(ev.Dynamic);
        var rules = EntityQueryEnumerator<AntagSelectionComponent>();

        var roleMap = new Dictionary<string, List<(EntityUid Entity, AntagSelectionComponent Comp)>>();

        while (rules.MoveNext(out var uid, out var selection))
        {
            foreach (var def in selection.Definitions)
            {
                var allRoles = def.PrefRoles.Select(p => p.Id);

                foreach (var role in allRoles)
                {
                    if (!roleMap.TryGetValue(role, out var list))
                    {
                        list = new List<(EntityUid, AntagSelectionComponent)>();
                        roleMap[role] = list;
                    }

                    list.Add((uid, selection));
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
    }

    private void ApplyDynamicPrice(EntityUid store, IReadOnlyList<ListingDataWithCostModifiers> listings, ProtoId<DynamicPrototype> currentDynamic)
    {
        var itemDiscounts = _discount.GetItemsDiscount(store, listings);
        var discountsLookup = itemDiscounts.ToDictionary(d => d.ListingId, d => d);

        foreach (var listing in listings)
        {
            if (!listing.DynamicsPrices.TryGetValue(currentDynamic, out var dynamicPrice))
                continue;

            var nameModifier= nameof(listing.DynamicsPrices);
            listing.RemoveCostModifier(nameModifier);
            listing.SetExactPrice(nameModifier, dynamicPrice);

            if (!listing.DiscountCategory.HasValue)
                continue;

            if (!discountsLookup.TryGetValue(listing.ID, out var itemDiscount))
                continue;

            listing.RemoveCostModifier(listing.DiscountCategory.Value);
            var discountModifier = GetDiscountModifier(dynamicPrice, listing, itemDiscount);

            listing.AddCostModifier(listing.DiscountCategory.Value, discountModifier);
        }
    }

    private Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> GetDiscountModifier(
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
            var roundedValue =  rawValue+0.5; //Add 0.5 to round to nearest int
            finalPrice[discount.Key] = -roundedValue.Int();
        }

        return new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(finalPrice);
    }

    /// <summary>
    /// Gets and sets a random dynamic based on the current number of ready players.
    /// </summary>
    public void SetRandomDynamic(EntityUid? station = null)
    {
        var countPlayers = _gameTicker.ReadyPlayerCount();
        var dynamic = GetRandomDynamic(countPlayers, station: station);
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

        if (dynamicProto.LoreNames == default || !_prototype.TryIndex(dynamicProto.LoreNames, out var datasetPrototype))
            return;

        dynamicProto.SelectedLoreName = _random.Pick(datasetPrototype);
    }

    public void SetDynamic(string proto)
    {
        if (!_prototype.Resolve<DynamicPrototype>(proto, out var dynamicProto))
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
    public string GetRandomDynamic(int playerCount = 0, bool force = false, EntityUid? station = null)
    {
        var prototypeWeights = _prototype.Index<WeightedRandomPrototype>(_weightsProto);
        var resultWeights = new Dictionary<string, float>();
        (var _, var crewManifestEntries) = station is not null ? _crewManifest.GetCrewManifest(station.Value) : (null, null);

        foreach (var (id, weight) in prototypeWeights.Weights)
        {
            if (!_prototype.TryIndex<DynamicPrototype>(id, out var dynamicProto))
                continue;

            if (!(playerCount >= dynamicProto.PlayersRequirement))
                continue;

            var requirementMatch = true;
            foreach (var (departmentId, playerLimit) in dynamicProto.DepartmentLimits)
            {
                if (!_prototype.Resolve(departmentId, out var departmentPrototype))
                    continue;

                if (crewManifestEntries is null)
                    continue;

                if (crewManifestEntries.Entries.Count(x => departmentPrototype.Roles.Contains(x.JobPrototype)) < playerLimit)
                    requirementMatch = false;
            }

            if (!requirementMatch)
                continue;

            resultWeights.Add(id, weight);
        }

        var selectedDynamic = (playerCount == 0 || force) ? _random.Pick(prototypeWeights.Weights) : _random.Pick(resultWeights);

        return selectedDynamic;
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
