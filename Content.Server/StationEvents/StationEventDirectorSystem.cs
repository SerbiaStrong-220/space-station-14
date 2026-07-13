using Content.Server.AlertLevel;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

// SS220-event-director-begin
/// <summary>
/// Owns the station's random-event economy. Unlike legacy schedulers, this system is the only
/// normal producer of mid-round events: it earns one shared budget and buys one affordable event.
/// </summary>
public sealed class StationEventDirectorSystem : GameRuleSystem<StationEventDirectorComponent>
{
    private static readonly TimeSpan FailedAttemptDelay = TimeSpan.FromMinutes(1);

    [Dependency] private readonly EventManagerSystem _events = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        SubscribeLocalEvent<StationEventDirectorComponent, StationEventDirectorBudgetEvent>(OnBudgetChanged);
    }

    protected override void Started(EntityUid uid, StationEventDirectorComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.Budget = component.StartingBudget;
        component.LastBudgetUpdate = Timing.CurTime;
        component.NextEventTime = Timing.CurTime + GetEventDelay(component);
    }

    protected override void ActiveTick(EntityUid uid, StationEventDirectorComponent component, GameRuleComponent gameRule,
        float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!_events.EventsEnabled || Timing.CurTime < component.NextEventTime)
            return;

        UpdateBudget((uid, component));

        if (HasActiveStationEvent())
        {
            component.NextEventTime = Timing.CurTime + FailedAttemptDelay;
            return;
        }

        if (!_events.TryRunRandomEvent(component.ScheduledGameRules, component.Budget, out var cost))
        {
            component.NextEventTime = Timing.CurTime + FailedAttemptDelay;
            return;
        }

        component.Budget -= cost;
        component.NextEventTime = Timing.CurTime + GetEventDelay(component);
    }

    /// <summary>
    /// Adjusts the event budget. Gameplay systems may raise <see cref="StationEventDirectorBudgetEvent"/>
    /// on the active director to report a player-caused change in station pressure.
    /// </summary>
    public void AdjustBudget(Entity<StationEventDirectorComponent> director, float amount)
    {
        UpdateBudget(director);
        director.Comp.Budget = Math.Clamp(director.Comp.Budget + amount, 0f, director.Comp.MaximumBudget);
    }

    private void OnBudgetChanged(Entity<StationEventDirectorComponent> director, ref StationEventDirectorBudgetEvent args)
    {
        AdjustBudget(director, args.Amount);
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<StationEventDirectorComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var director, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            switch (args.AlertLevel)
            {
                case "green":
                    AdjustBudget((uid, director), director.RecoveryBudget);
                    break;
                case "blue":
                    DelayForAlert((uid, director), director.RecoveryBudget);
                    break;
                case "red":
                case "gamma":
                    DelayForAlert((uid, director), director.RecoveryBudget * 2f);
                    break;
            }
        }
    }

    private void DelayForAlert(Entity<StationEventDirectorComponent> director, float budgetPenalty)
    {
        AdjustBudget(director, -budgetPenalty);
        var delayedTime = Timing.CurTime + director.Comp.AlertBackoff;
        if (director.Comp.NextEventTime < delayedTime)
            director.Comp.NextEventTime = delayedTime;
    }

    private void UpdateBudget(Entity<StationEventDirectorComponent> director)
    {
        var elapsedMinutes = (float) (Timing.CurTime - director.Comp.LastBudgetUpdate).TotalMinutes;
        if (elapsedMinutes <= 0f)
            return;

        director.Comp.Budget = Math.Min(
            director.Comp.MaximumBudget,
            director.Comp.Budget + elapsedMinutes * director.Comp.BudgetPerMinute);
        director.Comp.LastBudgetUpdate = Timing.CurTime;
    }

    private bool HasActiveStationEvent()
    {
        var query = EntityQueryEnumerator<StationEventComponent, ActiveGameRuleComponent>();
        return query.MoveNext(out _, out _, out _);
    }

    private TimeSpan GetEventDelay(StationEventDirectorComponent component)
    {
        return _random.Next(component.MinimumEventDelay, component.MaximumEventDelay);
    }
}
// SS220-event-director-end
