using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.StationEvents.Components;

// SS220-event-director-begin
/// <summary>
/// Per-round state for the station event director. The component owns mutable pacing data;
/// <see cref="StationEventDirectorSystem"/> only evaluates and updates it.
/// </summary>
[RegisterComponent]
public sealed partial class StationEventDirectorComponent : Component
{
    [DataField]
    public StationEventSeverity Phase = StationEventSeverity.Calm;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPhaseUpdate;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? LastEvent;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? LastIncident;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? LastCrisis;
}
// SS220-event-director-end
