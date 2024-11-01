// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph;

[Prototype("surgeryGraph")]
public sealed partial class SurgeryGraphPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Start { get; private set; } = default!;

    [DataField(required: true)]
    public string End { get; private set; } = default!;

    [DataField("graph", priority: 0)]
    private List<SurgeryGraphNode> _graph = new();


    public bool GetStartNode([NotNullWhen(true)] out SurgeryGraphNode? startNode)
    {
        if (!TryGetNode(Start, out startNode))
            return false;

        return true;
    }

    public SurgeryGraphNode? GetEndNode()
    {
        TryGetNode(End, out var endNode);
        return endNode;
    }

    public bool GetEndNode([NotNullWhen(true)] out SurgeryGraphNode? endNode)
    {
        if (!TryGetNode(End, out endNode))
            return false;

        return true;
    }
    public bool TryGetNode(string target, [NotNullWhen(true)] out SurgeryGraphNode? findedNode)
    {
        findedNode = null;
        foreach (var node in _graph)
        {
            if (node.Name == target)
            {
                findedNode = node;
                break;
            }
        }

        return findedNode != null;
    }
}
