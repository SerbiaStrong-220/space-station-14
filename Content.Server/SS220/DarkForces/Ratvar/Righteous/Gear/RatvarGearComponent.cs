using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Gear;

[RegisterComponent]
public sealed partial class RatvarGearComponent : Component
{
    [DataField]
    public bool Active;

    [DataField(customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan NextTick = TimeSpan.Zero;

    [DataField]
    public int Power;

    [DataField]
    public int PowerPerTick = 2;
}
