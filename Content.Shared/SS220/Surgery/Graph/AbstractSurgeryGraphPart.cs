// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph;

[Prototype("abstractSurgeryNode")]
public sealed partial class AbstractSurgeryNodePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(priority: 0)]
    public SurgeryGraphNode Node = new();
}

[Prototype("abstractSurgeryEdge")]
public sealed partial class AbstractSurgeryEdgePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(priority: 0)]
    public SurgeryGraphEdge Edge = new();
}
