using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents;

public sealed class RampingStationEventSchedulerSystem : GameRuleSystem<RampingStationEventSchedulerComponent>
{
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly StationEventDirectorSystem _director = default!; // SS220-event-director

    protected override void Started(EntityUid uid, RampingStationEventSchedulerComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // SS220-event-director-begin
        // Kept as a separate prototype for map compatibility. The shared director now owns pacing.
        component.NextEventTime = Timing.CurTime + TimeSpan.FromMinutes(3);
        // SS220-event-director-end
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_event.EventsEnabled)
            return;

        var query = EntityQueryEnumerator<RampingStationEventSchedulerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var scheduler, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (Timing.CurTime < scheduler.NextEventTime)
                continue;

            scheduler.NextEventTime = Timing.CurTime + _director.RequestEvent(scheduler.ScheduledGameRules); // SS220-event-director
        }
    }
}
