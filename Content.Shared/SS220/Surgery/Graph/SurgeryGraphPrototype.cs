// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Graph;

[Prototype]
public sealed partial class SurgeryGraphPrototype : IPrototype, ISerializationHooks
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Unique name to show in UI. Serves for user comfort orientation.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Detailed description of operation: what it does and etc. Could be lore-boxedish
    /// </summary>
    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// More gameplay specific information. Like if it needs a special tool or operation exclude other one.
    /// </summary>
    [DataField]
    public LocId? Postscript;

    [DataField]
    public SurgeryGraphRequirement[] Requirements = Array.Empty<SurgeryGraphRequirement>();

    [DataField(required: true)]
    public string Start { get; private set; } = default!;

    [DataField(required: true)]
    public string End { get; private set; } = default!;

    [DataField(required: true)]
    public SurgeryPuppetPart TargetPart;

    [DataField("graph", priority: 0)]
    private SurgeryGraphNode[] _graph = Array.Empty<SurgeryGraphNode>();

    public IReadOnlyList<SurgeryGraphNode> Nodes => _graph;


    public SurgeryGraphNode? GetStartNode()
    {
        TryGetNode(Start, out var startNode);
        return startNode!;
    }

    public SurgeryGraphNode GetEndNode()
    {
        TryGetNode(End, out var endNode);
        return endNode!;
    }

    public bool GetEndNode([NotNullWhen(true)] out SurgeryGraphNode? endNode)
    {
        if (!TryGetNode(End, out endNode))
            return false;

        return true;
    }
    public bool TryGetNode(string target, [NotNullWhen(true)] out SurgeryGraphNode? foundNode)
    {
        foundNode = null;
        foreach (var node in _graph)
        {
            if (node.Name == target)
            {
                foundNode = node;
                break;
            }
        }

        return foundNode != null;
    }

    void ISerializationHooks.AfterDeserialization()
    {
        if (!TryGetNode(Start, out var _))
            throw new Exception($"No start node in surgery graph {ID}");

        if (!TryGetNode(End, out var _))
            throw new Exception($"No end node in surgery graph {ID}");
    }
}
