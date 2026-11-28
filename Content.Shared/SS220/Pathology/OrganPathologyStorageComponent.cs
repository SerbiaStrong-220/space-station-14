// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Pathologies of an extracted organ. They are restored on the body the organ is inserted into.
/// </summary>
[RegisterComponent]
public sealed partial class OrganPathologyStorageComponent : Component
{
    [ViewVariables]
    public Dictionary<ProtoId<PathologyPrototype>, PathologyInstanceData> Pathologies = new();
}
