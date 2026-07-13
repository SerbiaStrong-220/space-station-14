// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.PhysicalParameters;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class NeuralInterfaceComponent : Component
{
    [DataField, AutoNetworkedField]
    public int InterfaceCapacityRating = 10;

    [DataField]
    public ProtoId<AlertPrototype> InterfaceAlertProto = "NeuralInterface";

    [DataField, AutoNetworkedField]
    public int InterfaceType = 0;
}
