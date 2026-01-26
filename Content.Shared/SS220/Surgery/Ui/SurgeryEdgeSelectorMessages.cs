// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Ui;

[Serializable, NetSerializable]
public sealed partial class SurgeryEdgeSelectorEdgeSelectedMessage : BoundUserInterfaceMessage
{
    public ProtoId<SurgeryGraphPrototype> SurgeryId { init; get; } = default;
    public string TargetNode { init; get; } = string.Empty;
    public NetEntity? Used;
}
