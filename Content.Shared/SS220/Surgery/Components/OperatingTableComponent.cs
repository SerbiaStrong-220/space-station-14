// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

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
    [AutoNetworkedField]
    public float LinkedComputerRange = 2.5f;
}
