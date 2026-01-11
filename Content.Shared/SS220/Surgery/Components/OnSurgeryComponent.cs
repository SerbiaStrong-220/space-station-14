// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Components;

/// <summary>
/// This component contains information about being a valid target for surgery and ongiong operations
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryPatientComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SurgeryGraphPrototype>, string> OngoingSurgeries = new();
}
