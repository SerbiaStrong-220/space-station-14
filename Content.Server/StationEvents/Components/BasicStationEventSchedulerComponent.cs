using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.EntitySelectors;


namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BasicStationEventSchedulerSystem))]
public sealed partial class BasicStationEventSchedulerComponent : Component
{
    /// <summary>
    /// How long the the scheduler waits to begin starting rules.
    /// </summary>
    [DataField]
    // public float MinimumTimeUntilFirstEvent = 200; // [wizden-code]
    // It has hard-coded random addition +2 minutes so first event fire in [3, 5] minutes of round
    public float MinimumTimeUntilFirstEvent = 180f; //SS220-cut-round-duration

    /// <summary>
    /// The minimum and maximum time between rule starts in seconds.
    /// </summary>
    [DataField]
    // public MinMax MinMaxEventTiming = new(3 * 60, 10 * 60); // [wizden-code]
    public MinMax MinMaxEventTiming = new(60 * 5, 60 * 10); //SS220-cut-round-duration

    /// <summary>
    /// How long until the next check for an event runs, is initially set based on MinimumTimeUntilFirstEvent & MinMaxEventTiming.
    /// </summary>
    [DataField]
    public float TimeUntilNextEvent;

    /// <summary>
    /// The gamerules that the scheduler can choose from
    /// </summary>
    /// Reminder that though we could do all selection via the EntityTableSelector, we also need to consider various <see cref="StationEventComponent"/> restrictions.
    /// As such, we want to pass a list of acceptable game rules, which are then parsed for restrictions by the <see cref="EventManagerSystem"/>.
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;
}
