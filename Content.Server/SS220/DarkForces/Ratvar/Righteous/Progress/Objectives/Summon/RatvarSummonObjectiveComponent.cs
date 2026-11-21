using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress.Objectives.Summon;

[RegisterComponent]
public sealed partial class RatvarSummonObjectiveComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsCompleted;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Target;

    [DataField(customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan UpdateCoordinatesTime;

    [DataField]
    public TimeSpan UpdateCoordinatesPeriod = TimeSpan.FromSeconds(10);
}
