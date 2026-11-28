// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OperatingTableComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public float SpeedModifier = 0.85f;

    [DataField]
    [AutoNetworkedField]
    public float FailureModifier = -0.10f;

    [DataField]
    [AutoNetworkedField]
    public float LinkedComputerFailureBonus = -0.05f;

    [DataField]
    public ProtoId<SinkPortPrototype> ComputerPort = "OperatingTableReceiver";

    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? LinkedComputer;
}
