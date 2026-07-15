using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Stack;
using Content.Server.Store.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Interaction;
using Content.Shared.Roles;
using Content.Shared.SS220.Pirates;
using Content.Shared.Stacks;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pirates;

public sealed partial class PirateMarketSystem : EntitySystem
{
    private static readonly ProtoId<CurrencyPrototype> PirateCurrency = "PirateCredit";

    [Dependency] private StoreSystem _store = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _roles = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStorePurchase);
        SubscribeLocalEvent<PirateMarketConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PirateMarketConsoleComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<PirateMarketConsoleComponent, CurrencyInsertAttemptEvent>(OnCurrencyInsertAttempt);
        SubscribeLocalEvent<PirateMarketConsoleComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
        SubscribeLocalEvent<CashComponent, AfterInteractEvent>(OnCashAfterInteract);
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

    private void OnCashAfterInteract(Entity<CashComponent> cash, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            !args.CanReach ||
            args.Target is not { } target ||
            !HasComp<PirateMarketConsoleComponent>(target) ||
            !TryComp<StoreComponent>(target, out var store) ||
            !TryComp<StackComponent>(cash, out var stack))
        {
            return;
        }

        var insertAttempt = new CurrencyInsertAttemptEvent(args.User, target, cash.Owner, store);
        RaiseLocalEvent(target, insertAttempt);
        if (insertAttempt.Cancelled)
        {
            args.Handled = true;
            return;
        }

        var currency = new Dictionary<string, FixedPoint2>
        {
            [PirateCurrency] = stack.Count,
        };

        if (!_store.TryAddCurrency(currency, target, store))
            return;

        _stack.SetCount((cash.Owner, stack), 0);
        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("store-currency-inserted", ("used", cash.Owner), ("target", target)),
            target,
            args.User);
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
        return _mind.TryGetMind(user, out var mind, out _) &&
               _roles.MindHasRole<PirateCrewRoleComponent>(mind);
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
