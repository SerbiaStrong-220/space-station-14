using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.SS220.Pirates;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pirates;

public sealed partial class PirateMarketSystem : EntitySystem
{
    private static readonly ProtoId<NpcFactionPrototype> PirateFaction = "Syndicate";

    [Dependency] private StoreSystem _store = default!;
    [Dependency] private NpcFactionSystem _factions = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStorePurchase);
        SubscribeLocalEvent<PirateMarketConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PirateMarketConsoleComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<PirateMarketConsoleComponent, CurrencyInsertAttemptEvent>(OnCurrencyInsertAttempt);
        SubscribeLocalEvent<PirateMarketConsoleComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
    }

    private void OnOpenAttempt(Entity<PirateMarketConsoleComponent> market, ref ActivatableUIOpenAttemptEvent args)
    {
        if (CanUseMarket(args.User))
            return;

        args.Cancel();
        if (!args.Silent)
            _popup.PopupEntity(Loc.GetString("pirate-market-access-denied"), market, args.User);
    }

    private void OnCurrencyInsertAttempt(Entity<PirateMarketConsoleComponent> market,
        ref CurrencyInsertAttemptEvent args)
    {
        if (CanUseMarket(args.User))
            return;

        args.Cancel();
        _popup.PopupEntity(Loc.GetString("pirate-market-access-denied"), market, args.User);
    }

    private void OnUiMessageAttempt(Entity<PirateMarketConsoleComponent> market,
        ref BoundUserInterfaceMessageAttempt args)
    {
        if (!args.UiKey.Equals(StoreUiKey.Key) || CanUseMarket(args.Actor))
            return;

        args.Cancel();
        _popup.PopupEntity(Loc.GetString("pirate-market-access-denied"), market, args.Actor);
    }

    private void OnUiOpened(Entity<PirateMarketConsoleComponent> market, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(StoreUiKey.Key) ||
            !TryComp<StoreComponent>(market, out var store) ||
            !TryGetRule(out var rule))
        {
            return;
        }

        ApplyPurchaseCounts(store, rule.Comp);
        _store.UpdateUserInterface(args.Actor, market, store);
    }

    private void OnStorePurchase(ref StoreBuyFinishedEvent args)
    {
        if (!HasComp<PirateMarketConsoleComponent>(args.StoreUid))
            return;

        var purchaseAmount = args.PurchasedItem.PurchaseAmount;
        if (TryGetRule(out var rule))
        {
            var listingId = new ProtoId<ListingPrototype>(args.PurchasedItem.ID);
            rule.Comp.MarketPurchases.TryGetValue(listingId, out purchaseAmount);
            purchaseAmount++;
            rule.Comp.MarketPurchases[listingId] = purchaseAmount;
        }

        var markets = EntityQueryEnumerator<PirateMarketConsoleComponent, StoreComponent>();
        while (markets.MoveNext(out var market, out _, out var store))
        {
            foreach (var listing in store.FullListingsCatalog)
            {
                if (listing.ID != args.PurchasedItem.ID)
                    continue;

                listing.PurchaseAmount = purchaseAmount;
                _store.UpdateUserInterface(args.User, market, store);
                break;
            }
        }
    }

    private static void ApplyPurchaseCounts(StoreComponent store, PirateGameRuleComponent rule)
    {
        foreach (var listing in store.FullListingsCatalog)
        {
            var listingId = new ProtoId<ListingPrototype>(listing.ID);
            if (rule.MarketPurchases.TryGetValue(listingId, out var purchaseAmount))
                listing.PurchaseAmount = purchaseAmount;
        }
    }

    private bool CanUseMarket(EntityUid user)
    {
        return _factions.IsMember(user, PirateFaction);
    }

    private bool TryGetRule(out Entity<PirateGameRuleComponent> rule)
    {
        var query = EntityQueryEnumerator<PirateGameRuleComponent, ActiveGameRuleComponent>();
        if (query.MoveNext(out var uid, out var component, out _))
        {
            rule = (uid, component);
            return true;
        }

        rule = default;
        return false;
    }
}
