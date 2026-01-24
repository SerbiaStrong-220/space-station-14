// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Surgery.Ui;

[Serializable, NetSerializable]
public sealed partial class SurgeryEdgeSelectorEdgesState : BoundUserInterfaceState
{
    public List<EdgeSelectInfo> Infos { init; get; } = new();
}

[Serializable, NetSerializable]
public sealed partial class EdgeSelectInfo(string targetNode, ProtoId<SurgeryGraphPrototype> surgeryProtoId, LocId tooltip, bool metEdgeRequirement, SpriteSpecifier? icon)
{
    public string TargetNode = targetNode;
    public ProtoId<SurgeryGraphPrototype> SurgeryProtoId = surgeryProtoId;
    public LocId Tooltip = tooltip;
    public bool MetEdgeRequirement = metEdgeRequirement;
    public SpriteSpecifier? Icon = icon;
};
