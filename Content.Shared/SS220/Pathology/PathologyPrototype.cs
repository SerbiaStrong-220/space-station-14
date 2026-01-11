// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed class PathologyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public PathologyDefinition[] Definition = Array.Empty<PathologyDefinition>();

}

[DataDefinition]
public sealed partial class PathologyDefinition
{
    [DataField(required: true)]
    public LocId Description;

    [DataField]
    public LocId? ProgressPopup;

    [DataField]
    public ComponentRegistry Components;

    [DataField]
    public PathologyProgressCondition[] ProgressConditions = Array.Empty<PathologyProgressCondition>();

    [DataField]
    public IPathologyEffect[] Effects;
}
