using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Pirates;
using Content.Shared.Store.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pirates;

public sealed partial class PirateLootSystem : EntitySystem
{
    private static readonly EntProtoId CashPrototype = "SpaceCash";

    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<PirateLootConsoleComponent>(PirateLootConsoleUiKey.Key, subs =>
        {
            subs.Event<PirateLootAppraiseMessage>(OnAppraise);
            subs.Event<PirateLootSellMessage>(OnSell);
        });
        SubscribeLocalEvent<PirateLootConsoleComponent, BoundUIOpenedEvent>(OnOpened);
    }

    private void OnOpened(Entity<PirateLootConsoleComponent> console, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey.Equals(PirateLootConsoleUiKey.Key))
            UpdateUi(console);
    }

    private void OnSell(Entity<PirateLootConsoleComponent> console, ref PirateLootSellMessage args)
    {
        if (!TryGetRule(out var rule))
        {
            _popup.PopupEntity(Loc.GetString("pirate-loot-no-active-crew"), console, args.Actor);
            return;
        }

        if (!TryGetSale(console, out var goods, out var value))
            return;

        foreach (var entity in goods)
            Del(entity);

        _stack.SpawnMultipleAtPosition(CashPrototype, value, Transform(console).Coordinates);
        rule.Comp.TotalLootValue += value;
        rule.Comp.TotalItemsSold += goods.Count;
        _popup.PopupEntity(Loc.GetString("pirate-loot-sale-complete", ("amount", value)), console, args.Actor);
        UpdateUi(console);
    }

    private void OnAppraise(Entity<PirateLootConsoleComponent> console, ref PirateLootAppraiseMessage args)
    {
        UpdateUi(console);
    }

    private void UpdateUi(Entity<PirateLootConsoleComponent> console)
    {
        TryGetSale(console, out var goods, out var value);
        var totalLootValue = 0L;
        var totalItemsSold = 0;
        if (TryGetRule(out var rule))
        {
            totalLootValue = rule.Comp.TotalLootValue;
            totalItemsSold = rule.Comp.TotalItemsSold;
        }

        _ui.SetUiState(console.Owner, PirateLootConsoleUiKey.Key,
            new PirateLootConsoleState(value,
                goods.Count,
                totalLootValue,
                totalItemsSold));
    }

    private bool TryGetSale(Entity<PirateLootConsoleComponent> console, out HashSet<EntityUid> goods, out int total)
    {
        goods = new HashSet<EntityUid>();
        total = 0;
        var totalPrice = 0d;
        if (Transform(console).GridUid is not { } grid)
            return false;

        var pads = EntityQueryEnumerator<PirateLootPadComponent, TransformComponent>();
        while (pads.MoveNext(out var pad, out _, out var padXform))
        {
            if (padXform.GridUid != grid || !padXform.Anchored)
                continue;

            var candidates = _lookup.GetEntitiesIntersecting(pad, LookupFlags.Dynamic | LookupFlags.Sundries);
            foreach (var candidate in candidates)
            {
                if (candidate == pad ||
                    goods.Contains(candidate) ||
                    !TryComp(candidate, out TransformComponent? candidateXform) ||
                    candidateXform.Anchored ||
                    ContainsRestrictedEntity(candidate, candidateXform))
                {
                    continue;
                }

                var price = _pricing.GetPrice(candidate);
                if (price <= 0 || !double.IsFinite(price))
                    continue;

                goods.Add(candidate);
                totalPrice += price;
            }
        }

        total = (int) Math.Min(int.MaxValue, totalPrice);
        return goods.Count > 0 && total > 0;
    }

    private bool ContainsRestrictedEntity(EntityUid entity, TransformComponent xform)
    {
        if (HasComp<MobStateComponent>(entity) || HasComp<CurrencyComponent>(entity))
            return true;

        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (ContainsRestrictedEntity(child, Transform(child)))
                return true;
        }

        return false;
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
