// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.FieldShield;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FieldShieldProviderComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public ShieldData ShieldData = default;

    [DataField]
    [AutoNetworkedField]
    public RechargeShieldData RechargeShieldData = default;

    [DataField]
    [AutoNetworkedField]
    public ShieldLightData LightData = default;
}
