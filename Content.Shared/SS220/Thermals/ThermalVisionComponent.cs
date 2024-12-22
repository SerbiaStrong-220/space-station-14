// Original code github.com/CM-14 Licence MIT, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Thermals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ThermalVisionComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> Alert = "ThermalVision";

    [DataField, AutoNetworkedField]
    public ThermalVisionState State = ThermalVisionState.Half;

    [ViewVariables, AutoNetworkedField]
    public float ThermalVisionRadius = 8f;

    public ThermalVisionComponent(float radius)
    {
        ThermalVisionRadius = radius;
    }
}

[Serializable, NetSerializable]
public enum ThermalVisionState
{
    Off,
    Half,
    Full
}

public sealed partial class UseThermalVisionEvent : InstantActionEvent
{

}
