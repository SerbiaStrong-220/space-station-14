// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Graph;

[Serializable]
[DataDefinition]
public sealed partial class SurgeryGraphNode : ISerializationHooks
{
    [DataField("node", required: true)]
    public LocId Name { get; private set; } = default!;

    [DataField]
    public ProtoId<AbstractSurgeryNodePrototype>? BaseNode { get; private set; }

    [DataField]
    [Access(typeof(SurgeryGraphSystem), Other = AccessPermissions.None)]
    public NodeTextDescription NodeText = new();

    [DataField("edges")]
    private SurgeryGraphEdge[] _edges= Array.Empty<SurgeryGraphEdge>();

    [ViewVariables]
    public IReadOnlyList<SurgeryGraphEdge> Edges => _edges;

    void ISerializationHooks.AfterDeserialization()
    {
        if (_edges.Select(x => x.Id).ToHashSet().Count == _edges.Length)
            return;

        throw new Exception($"Got several edges with same id in node named {Name}!");
    }
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
