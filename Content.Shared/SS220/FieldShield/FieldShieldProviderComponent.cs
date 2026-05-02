// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.FieldShield;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class FieldShieldProviderComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public bool LockOnEmp = true;

    [DataField]
    [AutoNetworkedField]
    public EntityUid? Wearer = null;

    [DataField]
    [AutoNetworkedField]
    public Dictionary<string, FieldShieldData> Modes = new Dictionary<string, FieldShieldData> { };

    [ViewVariables]
    [AutoNetworkedField]
    public TimeSpan UnLockAfterEmpTime;

    [DataField(required: true)]
    [AutoNetworkedField]
    public FieldShieldData ShieldData;

    [DataField(required: true)]
    [AutoNetworkedField]
    public FieldShieldRechargeData RechargeShieldData;

    [DataField(required: true)]
    [AutoNetworkedField]
    public FieldShieldLightData LightData;
}
