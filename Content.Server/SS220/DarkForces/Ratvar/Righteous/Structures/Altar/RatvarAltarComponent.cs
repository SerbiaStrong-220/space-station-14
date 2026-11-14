using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Altar;

[RegisterComponent]
public sealed partial class RatvarAltarComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan ActivateTime = TimeSpan.Zero;

    [DataField]
    public EntityUid BuckledEntity = EntityUid.Invalid;

    [DataField]
    public EntProtoId SoulVessel = "RatvarSoulVessel";

    [DataField]
    public AltarActiveType Type = AltarActiveType.Idle;
}

public enum AltarActiveType
{
    Idle,
    Convert,
    Die
}
