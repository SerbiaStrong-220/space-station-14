using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.SS220.DarkForces.Ratvar;

[RegisterComponent]
[Access(typeof(RatvarRuleSystem))]
public sealed partial class RatvarRuleComponent : Component
{
    [DataField]
    public WinState WinState = WinState.Idle;

    [DataField(customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan ForceRoundEnd;
}

public enum WinState
{
    Idle = 0,
    Summoning = 1,
    RighteousWon = 2
}
