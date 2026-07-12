using Content.Server.GameTicking;
using Content.Server.StationEvents.Components;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.StationEvents;

/// <summary>
/// A small utility-AI that gives random station events a shared rhythm.  It deliberately
/// coordinates all schedulers in a round, rather than letting each scheduler build its own
/// escalating event queue.
/// </summary>
// SS220-event-director-begin
public sealed class StationEventDirectorSystem : EntitySystem
{
    private static readonly TimeSpan AnyEventCooldown = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan IncidentCooldown = TimeSpan.FromMinutes(6);
    private static readonly TimeSpan CrisisCooldown = TimeSpan.FromMinutes(25);
    private static readonly TimeSpan PhaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DeferredRetry = TimeSpan.FromMinutes(1);
    private static readonly EntProtoId DirectorPrototype = "StationEventDirector";

    [Dependency] private readonly EventManagerSystem _events = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartedEvent _)
    {
        var query = EntityQueryEnumerator<StationEventDirectorComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            Del(uid);
        }
    }

    /// <summary>
    /// Attempts one random event from a scheduler table and returns when it should try again.
    /// </summary>
    public TimeSpan RequestEvent(EntityTableSelector table)
    {
        var now = _timing.CurTime;
        var director = GetDirector();
        var state = director.Comp;
        UpdatePhase(state, now);

        if (state.LastEvent is { } lastEvent && now - lastEvent < AnyEventCooldown)
            return AnyEventCooldown - (now - lastEvent);

        var maximum = GetMaximumSeverity(state, now);
        if (!_events.TryRunRandomEvent(table, maximum, out var severity))
            return DeferredRetry;

        state.LastEvent = now;
        if (severity >= StationEventSeverity.Incident)
            state.LastIncident = now;
        if (severity == StationEventSeverity.Crisis)
            state.LastCrisis = now;

        return GetNextAttemptDelay(state);
    }

    private Entity<StationEventDirectorComponent> GetDirector()
    {
        var query = EntityQueryEnumerator<StationEventDirectorComponent>();
        if (query.MoveNext(out var uid, out var component))
            return (uid, component);

        var director = Spawn(DirectorPrototype, MapCoordinates.Nullspace);
        return (director, Comp<StationEventDirectorComponent>(director));
    }

    private void UpdatePhase(StationEventDirectorComponent state, TimeSpan now)
    {
        if (now < state.NextPhaseUpdate)
            return;

        state.NextPhaseUpdate = now + PhaseDuration;

        if (HasActiveThreat(StationEventSeverity.Crisis) || IsOnCooldown(state.LastCrisis, CrisisCooldown, now))
        {
            state.Phase = StationEventSeverity.Calm;
            return;
        }

        if (HasActiveThreat(StationEventSeverity.Incident))
        {
            state.Phase = StationEventSeverity.Calm;
            return;
        }

        var minutes = _gameTicker.RoundDuration().TotalMinutes;
        state.Phase = minutes switch
        {
            < 25 => PickPhase(70, 30, 0),
            < 60 => PickPhase(45, 45, 10),
            _ => PickPhase(30, 50, 20),
        };
    }

    private StationEventSeverity GetMaximumSeverity(StationEventDirectorComponent state, TimeSpan now)
    {
        if (state.Phase == StationEventSeverity.Crisis && !IsOnCooldown(state.LastCrisis, CrisisCooldown, now))
            return StationEventSeverity.Crisis;

        if (state.Phase >= StationEventSeverity.Incident && !IsOnCooldown(state.LastIncident, IncidentCooldown, now))
            return StationEventSeverity.Incident;

        return StationEventSeverity.Calm;
    }

    private StationEventSeverity PickPhase(int calm, int incident, int crisis)
    {
        var roll = _random.Next(0, calm + incident + crisis);
        if (roll < calm)
            return StationEventSeverity.Calm;
        if (roll < calm + incident)
            return StationEventSeverity.Incident;
        return StationEventSeverity.Crisis;
    }

    private bool HasActiveThreat(StationEventSeverity severity)
    {
        var query = EntityQueryEnumerator<StationEventComponent, ActiveGameRuleComponent>();
        while (query.MoveNext(out _, out var stationEvent, out _))
        {
            if (stationEvent.DirectorSeverity >= severity)
                return true;
        }

        return false;
    }

    private static bool IsOnCooldown(TimeSpan? last, TimeSpan cooldown, TimeSpan now)
    {
        return last is { } time && now - time < cooldown;
    }

    private TimeSpan GetNextAttemptDelay(StationEventDirectorComponent state)
    {
        return state.Phase switch
        {
            StationEventSeverity.Calm => TimeSpan.FromMinutes(_random.Next(5, 9)),
            StationEventSeverity.Incident => TimeSpan.FromMinutes(_random.Next(4, 7)),
            _ => TimeSpan.FromMinutes(_random.Next(3, 6)),
        };
    }
}
// SS220-event-director-end
