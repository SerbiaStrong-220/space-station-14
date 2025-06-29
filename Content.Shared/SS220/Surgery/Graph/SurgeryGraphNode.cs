// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph;

[Serializable]
[DataDefinition]
public sealed partial class SurgeryGraphNode
{
    [DataField("node", required: true)]
    public string Name { get; private set; } = default!;

    [DataField]
    public ProtoId<AbstractSurgeryNodePrototype>? BaseNode { get; private set; }

    [DataField]
    public NodeTextDescription NodeText = new();

    [DataField("edges")]
    private SurgeryGraphEdge[] _edges = Array.Empty<SurgeryGraphEdge>();

    [ViewVariables]
    public IReadOnlyList<SurgeryGraphEdge> Edges => _edges;

}

[DataDefinition]
public sealed partial class NodeTextDescription
{
    [DataField]
    public string? ExamineDescription;

    [DataField]
    public string? Popup;

    [DataField]
    public string? Description;
}
