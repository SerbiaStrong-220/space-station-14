using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(RampingStationEventSchedulerSystem))]
public sealed partial class RampingStationEventSchedulerComponent : Component
{
    // SS220-event-director-begin
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEventTime;
    // SS220-event-director-end

    /// <summary>
    /// The gamerules that the scheduler can choose from
    /// </summary>
    /// Reminder that though we could do all selection via the EntityTableSelector, we also need to consider various <see cref="StationEventComponent"/> restrictions.
    /// As such, we want to pass a list of acceptable game rules, which are then parsed for restrictions by the <see cref="EventManagerSystem"/>.
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;
}
