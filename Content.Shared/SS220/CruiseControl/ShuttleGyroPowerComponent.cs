using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CruiseControl;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShuttleGyroPowerComponent : Component
{
    public const float MinPower = 0.1f;
    public const float MaxPower = 2f;

    [DataField, AutoNetworkedField, ViewVariables]
    public float Power = 1f;
}
