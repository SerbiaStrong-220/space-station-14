// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

[Serializable, NetSerializable]
public sealed partial class SurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public ProtoId<SurgeryGraphPrototype> SurgeryGraph;

    public string TargetEdge;

    public SurgeryDoAfterEvent(ProtoId<SurgeryGraphPrototype> surgeryGraph, string targetEdge) : base()
    {
        SurgeryGraph = surgeryGraph;
        TargetEdge = targetEdge;
    }
}
