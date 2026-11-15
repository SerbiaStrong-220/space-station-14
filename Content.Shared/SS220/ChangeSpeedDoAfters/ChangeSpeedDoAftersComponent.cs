using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ChangeSpeedDoAfters;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangeSpeedDoAftersComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Coefficient = 1f;

    [DataField, AutoNetworkedField]
    public float? ChanceToFail;

    public Dictionary<int, TimeSpan> ScheduledCancelTimes = new();
}
