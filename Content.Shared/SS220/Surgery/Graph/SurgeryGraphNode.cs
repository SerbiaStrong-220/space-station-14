// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Surgery.Graph;

[Serializable]
[DataDefinition]
public sealed partial class SurgeryGraphNode
{
    [DataField("node", required: true)]
    public string Name { get; private set; } = default!;

    [DataField("edges")]
    private SurgeryGraphEdge[] _edges = Array.Empty<SurgeryGraphEdge>();

    [DataField("description")]
    private string _description = string.Empty;

    [DataField("popup")]
    private string _popup = string.Empty;

    /// <summary>
    /// Already Localized string of node description
    /// </summary>
    [ViewVariables]
    public string Description => Loc.GetString(_description);

    /// <summary>
    /// Already Localized string, which is used in popups at surgery target after node reached
    /// </summary>
    [ViewVariables]
    public string Popup => Loc.GetString(_popup);

    [ViewVariables]
    public IReadOnlyList<SurgeryGraphEdge> Edges => _edges;

}
