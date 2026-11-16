using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ChangeSpeedDoAfters;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class ChangeSpeedDoAftersComponent : Component
{
    [DataField]
    public float Coefficient;

    [DataField]
    public float? ChanceToFail;

    public Dictionary<int, TimeSpan> ScheduledCancelTimes = new();
}
