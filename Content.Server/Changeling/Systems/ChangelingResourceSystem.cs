// SS220 Changeling
using System.Linq;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Changeling.Systems;
using Content.Shared.Clumsy;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Systems;

/// <summary>
/// Authoritative API and regeneration loop for changeling chemicals and evolution points.
/// </summary>
public sealed class ChangelingResourceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private static readonly ProtoId<CurrencyPrototype> EvolutionCurrency = "ChangelingEvolution";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingResourceComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ChangelingResourceComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingChemicalSpendAttemptEvent>(OnChemicalSpendAttempt);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingAddChemicalsEvent>(OnAddChemicals);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingResetEvolutionEvent>(OnResetEvolution);
        SubscribeLocalEvent<ChangelingResourceComponent, BoundUserInterfaceMessageAttempt>(OnBoundUiMessageAttempt);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStorePurchase);
    }

    private void OnBoundUiMessageAttempt(
        Entity<ChangelingResourceComponent> ent,
        ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.Actor == ent.Owner)
            return;

        if (args.UiKey.Equals(StoreUiKey.Key) ||
            args.UiKey.Equals(ChangelingApexTrackerUiKey.Key) ||
            args.UiKey.Equals(ChangelingTransformUiKey.Key) ||
            args.UiKey.Equals(ChangelingTransformUiKey.TransformationSting))
        {
            args.Cancel();
        }
    }

    private void OnComponentInit(Entity<ChangelingResourceComponent> ent, ref ComponentInit args)
    {
        // A changeling's control over its own anatomy supersedes the clown job's clumsiness.
        RemComp<ClumsyComponent>(ent);

        ent.Comp.MaxChemicals = FixedPoint2.Max(FixedPoint2.Zero, ent.Comp.MaxChemicals);
        ent.Comp.Chemicals = FixedPoint2.Clamp(ent.Comp.Chemicals, FixedPoint2.Zero, ent.Comp.MaxChemicals);
        ent.Comp.ChemicalRegenerationAmount = FixedPoint2.Max(
            FixedPoint2.Zero,
            ent.Comp.ChemicalRegenerationAmount);
        if (ent.Comp.ChemicalRegenerationInterval < TimeSpan.Zero)
            ent.Comp.ChemicalRegenerationInterval = TimeSpan.Zero;
        ent.Comp.MaxEvolutionPoints = Math.Max(0, ent.Comp.MaxEvolutionPoints);
        ent.Comp.EvolutionPoints = TryComp<StoreComponent>(ent.Owner, out var store) &&
                                  TryGetEvolutionPoints(ent.Owner, store, out var evolutionPoints)
            ? evolutionPoints
            : Math.Clamp(ent.Comp.EvolutionPoints, 0, ent.Comp.MaxEvolutionPoints);
        ent.Comp.RegenerativeStasisChemicalCost = FixedPoint2.Max(
            FixedPoint2.Zero,
            ent.Comp.RegenerativeStasisChemicalCost);
        if (ent.Comp.RegenerativeStasisDuration < TimeSpan.Zero)
            ent.Comp.RegenerativeStasisDuration = TimeSpan.Zero;

        if (ent.Comp.ChemicalRegenerationInterval > TimeSpan.Zero &&
            ent.Comp.NextChemicalRegeneration <= TimeSpan.Zero)
        {
            ent.Comp.NextChemicalRegeneration = _timing.CurTime + ent.Comp.ChemicalRegenerationInterval;
        }

        Dirty(ent);
        _alerts.ShowAlert(ent.Owner, ent.Comp.ChemicalsAlert);
    }

    private void OnComponentRemove(Entity<ChangelingResourceComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.ChemicalsAlert);
        if (!TerminatingOrDeleted(ent.Owner))
        {
            _actions.RemoveAction(ent.Owner, ent.Comp.RegenerativeStasisActionEntity);
            _actions.RemoveAction(ent.Owner, ent.Comp.RegenerateActionEntity);
        }

        var removed = new ChangelingResourceRemovedEvent(TerminatingOrDeleted(ent.Owner));
        RaiseLocalEvent(ent.Owner, ref removed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ChangelingResourceComponent>();

        while (query.MoveNext(out var uid, out var resources))
        {
            if (TryComp<StoreComponent>(uid, out var store) &&
                TryGetEvolutionPoints(uid, store, out var evolutionPoints))
            {
                SetEvolutionPoints((uid, resources), evolutionPoints);
            }

            if (resources.ChemicalRegenerationInterval <= TimeSpan.Zero ||
                resources.ChemicalRegenerationAmount <= FixedPoint2.Zero)
                continue;

            if (resources.NextChemicalRegeneration == TimeSpan.Zero)
            {
                resources.NextChemicalRegeneration = now + resources.ChemicalRegenerationInterval;
                Dirty(uid, resources);
                continue;
            }

            if (now < resources.NextChemicalRegeneration)
                continue;

            var overdue = now - resources.NextChemicalRegeneration;
            var intervalTicks = resources.ChemicalRegenerationInterval.Ticks;
            var elapsedIntervals = overdue.Ticks / intervalTicks;
            var tickCount = elapsedIntervals == long.MaxValue
                ? long.MaxValue
                : elapsedIntervals + 1L;
            var remainingInterval = TimeSpan.FromTicks(intervalTicks - overdue.Ticks % intervalTicks);
            resources.NextChemicalRegeneration = now > TimeSpan.MaxValue - remainingInterval
                ? TimeSpan.MaxValue
                : now + remainingInterval;
            Dirty(uid, resources);

            if (resources.Chemicals >= resources.MaxChemicals)
                continue;

            var multiplier = GetChemicalRegenerationMultiplier((uid, resources));
            if (multiplier <= 0f)
                continue;

            // Clamp before converting back to FixedPoint2. A very small interval can produce more catch-up ticks
            // than an int-backed FixedPoint2 can represent, but regeneration never needs to exceed the remaining
            // capacity of the chemical pool.
            var scaledCents = resources.ChemicalRegenerationAmount.Value * (double) multiplier;
            if (!double.IsFinite(scaledCents) || scaledCents < 1d)
                continue;

            var perTick = FixedPoint2.FromCents((int) Math.Min(scaledCents, int.MaxValue));
            var missingCents = (long) resources.MaxChemicals.Value - resources.Chemicals.Value;
            var ticksUntilFull = (missingCents + perTick.Value - 1L) / perTick.Value;
            if (tickCount >= ticksUntilFull)
            {
                SetChemicals((uid, resources), resources.MaxChemicals);
                continue;
            }

            AddChemicals((uid, resources), perTick * (int) tickCount);
        }
    }

    private void OnChemicalSpendAttempt(Entity<ChangelingResourceComponent> ent,
        ref ChangelingChemicalSpendAttemptEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.Cancelled = !TrySpendChemicals(ent.AsNullable(), args.Amount);
    }

    private void OnAddChemicals(Entity<ChangelingResourceComponent> ent, ref ChangelingAddChemicalsEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.AmountAdded = AddChemicals(ent.AsNullable(), args.Amount);
    }

    private void OnResetEvolution(Entity<ChangelingResourceComponent> ent, ref ChangelingResetEvolutionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.Succeeded = ResetEvolution(ent.AsNullable());
    }

    private void OnStorePurchase(ref StoreBuyFinishedEvent args)
    {
        if (args.StoreUid != args.User ||
            !TryComp<ChangelingResourceComponent>(args.User, out var resources) ||
            !TryComp<StoreComponent>(args.StoreUid, out var store) ||
            !store.CurrencyWhitelist.Contains(EvolutionCurrency) ||
            !args.PurchasedItem.Cost.ContainsKey(EvolutionCurrency) ||
            !TryGetEvolutionPoints(args.StoreUid, store, out var remainingPoints))
        {
            return;
        }

        // StoreSystem has already validated and atomically subtracted the listing cost. Mirror its authoritative
        // post-purchase balance instead of attempting a second independent spend that could silently fail after
        // a refund, discount, or future pricing change.
        SetEvolutionPoints((args.User, resources), remainingPoints);
    }

    private void ResetEvolutionStore(Entity<StoreComponent> store, int targetPoints)
    {
        foreach (var purchase in store.Comp.BoughtEntities.ToArray())
        {
            if (!Exists(purchase))
                continue;

            _actions.RemoveAction(purchase);
            QueueDel(purchase);
        }

        store.Comp.BoughtEntities.Clear();
        store.Comp.BalanceSpent.Clear();
        store.Comp.Balance[EvolutionCurrency] = FixedPoint2.New(targetPoints);
        _store.RefreshAllListings(store.Comp);
        _store.UpdateUserInterface(store.Owner, store.Owner, store.Comp);
    }

    /// <summary>
    /// Atomically spends chemicals if the entity has enough.
    /// </summary>
    public bool TrySpendChemicals(Entity<ChangelingResourceComponent?> ent, FixedPoint2 amount)
    {
        if (amount < FixedPoint2.Zero)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.Chemicals < amount)
            return false;

        SetChemicals((ent.Owner, ent.Comp), ent.Comp.Chemicals - amount);
        return true;
    }

    /// <summary>
    /// Adds chemicals and clamps the result to the configured maximum.
    /// Returns the amount that was actually added.
    /// </summary>
    public FixedPoint2 AddChemicals(Entity<ChangelingResourceComponent?> ent, FixedPoint2 amount)
    {
        if (amount < FixedPoint2.Zero)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (!Resolve(ent, ref ent.Comp, false))
            return FixedPoint2.Zero;

        var oldValue = ent.Comp.Chemicals;
        SetChemicals((ent.Owner, ent.Comp), oldValue + amount);
        return ent.Comp.Chemicals - oldValue;
    }

    /// <summary>
    /// Requests synchronous mutation cleanup and then restores the full evolution budget.
    /// </summary>
    public bool ResetEvolution(Entity<ChangelingResourceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false) ||
            !TryComp<StoreComponent>(ent.Owner, out var store) ||
            !store.CurrencyWhitelist.Contains(EvolutionCurrency) ||
            !TryGetEvolutionPoints(ent.Owner, store, out var previousPoints))
        {
            return false;
        }

        var reset = new ChangelingEvolutionResetEvent(previousPoints, ent.Comp.MaxEvolutionPoints);
        RaiseLocalEvent(ent.Owner, ref reset);
        ResetEvolutionStore((ent.Owner, store), ent.Comp.MaxEvolutionPoints);
        SetEvolutionPoints((ent.Owner, ent.Comp), ent.Comp.MaxEvolutionPoints);
        return true;
    }

    /// <summary>
    /// Adds or replaces a keyed regeneration multiplier. Active multipliers are multiplied together.
    /// </summary>
    public bool SetChemicalRegenerationModifier(Entity<ChangelingResourceComponent?> ent,
        string key,
        float multiplier)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A regeneration modifier key is required.", nameof(key));
        if (!float.IsFinite(multiplier) || multiplier < 0f)
            throw new ArgumentOutOfRangeException(nameof(multiplier));

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        ent.Comp.ChemicalRegenerationModifiers[key] = multiplier;
        return true;
    }

    public bool RemoveChemicalRegenerationModifier(Entity<ChangelingResourceComponent?> ent, string key)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.ChemicalRegenerationModifiers.Remove(key);
    }

    public float GetChemicalRegenerationMultiplier(Entity<ChangelingResourceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0f;

        var multiplier = 1f;
        foreach (var modifier in ent.Comp.ChemicalRegenerationModifiers.Values)
        {
            multiplier *= modifier;
            if (!float.IsFinite(multiplier) || multiplier < 0f)
                return 0f;
        }

        return multiplier;
    }

    private void SetChemicals(Entity<ChangelingResourceComponent> ent, FixedPoint2 value)
    {
        var newValue = FixedPoint2.Clamp(value, FixedPoint2.Zero, ent.Comp.MaxChemicals);
        if (newValue == ent.Comp.Chemicals)
            return;

        var oldValue = ent.Comp.Chemicals;
        ent.Comp.Chemicals = newValue;
        Dirty(ent);
        _alerts.ShowAlert(ent.Owner, ent.Comp.ChemicalsAlert);

        var changed = new ChangelingChemicalsChangedEvent(oldValue, newValue);
        RaiseLocalEvent(ent.Owner, ref changed);
    }

    private void SetEvolutionPoints(Entity<ChangelingResourceComponent> ent, int value)
    {
        var newValue = Math.Max(0, value);
        if (newValue == ent.Comp.EvolutionPoints)
            return;

        var oldValue = ent.Comp.EvolutionPoints;
        ent.Comp.EvolutionPoints = newValue;
        Dirty(ent);

        var changed = new ChangelingEvolutionPointsChangedEvent(oldValue, newValue);
        RaiseLocalEvent(ent.Owner, ref changed);
    }

    private bool TryGetEvolutionPoints(EntityUid uid, StoreComponent store, out int points)
    {
        points = 0;
        if (!store.CurrencyWhitelist.Contains(EvolutionCurrency) ||
            !store.Balance.TryGetValue(EvolutionCurrency, out var balance))
        {
            return false;
        }

        if (balance.Value % 100 != 0)
            Log.Error($"Changeling evolution balance on {ToPrettyString(uid)} is fractional: {balance}.");

        points = Math.Max(0, balance.Int());
        return true;
    }
}
