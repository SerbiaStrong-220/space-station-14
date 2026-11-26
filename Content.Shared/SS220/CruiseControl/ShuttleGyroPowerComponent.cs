// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CruiseControl;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShuttleGyroPowerComponent : Component
{
    public const float MinPower = 0.1f;
    public const float MaxPower = 20f;

    [DataField, AutoNetworkedField, ViewVariables]
    public float Power = 1f;
}
