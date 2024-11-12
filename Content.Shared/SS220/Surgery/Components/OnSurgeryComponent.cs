// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Components;

/// <summary>
/// This component applies to entities on which operation is started.
/// </summary>
[RegisterComponent]
public sealed partial class OnSurgeryComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SurgeryGraphPrototype> SurgeryGraphProtoId;

    [ViewVariables]
    public SurgeryGraphNode? CurrentNode;
}
