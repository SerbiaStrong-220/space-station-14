// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MindSlave;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;

namespace Content.Server.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    protected bool IsValidTarget(EntityUid uid, out string? reason)
    {
        reason = null;
        if (HasComp<OnSurgeryComponent>(uid)
            || !HasComp<MindSlaveComponent>(uid) // for now only for slaves
            || !HasComp<SurgableComponent>(uid)
            || !_buckleSystem.IsBuckled(uid))
            return false;

        return true;
    }

    protected bool IsValidPerformer(EntityUid uid)
    {
        if (!HasComp<MindSlaveMasterComponent>(uid)) // for now only for masters
            return false;

        return true;
    }

    protected bool OperationEnded(Entity<OnSurgeryComponent> entity)
    {
        var surgeryProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);
        if (entity.Comp.CurrentNode != surgeryProto.GetEndNode())
            return false;

        return true;
    }

    protected void ChangeSurgeryNode(Entity<OnSurgeryComponent> entity, string targetNode, EntityUid performer, EntityUid? used)
    {
        var surgeryProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);
        ChangeSurgeryNode(entity, performer, used, targetNode, surgeryProto);
    }

    protected void StartSurgeryNode(Entity<OnSurgeryComponent> entity, EntityUid performer, EntityUid? used)
    {
        var surgeryProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);
        ChangeSurgeryNode(entity, performer, used, surgeryProto.Start, surgeryProto);
    }

    // Think of way of the bulling the used parameter.
    protected void ChangeSurgeryNode(Entity<OnSurgeryComponent> entity, EntityUid performer, EntityUid? used, string targetNode, SurgeryGraphPrototype surgeryGraph)
    {
        if (!surgeryGraph.TryGetNode(targetNode, out var foundNode))
        {
            Log.Error($"No start node on graph {entity.Comp.SurgeryGraphProtoId} with name {targetNode}");
            return;
        }
        entity.Comp.CurrentNode = foundNode;
        if (SurgeryGraph.Popup(foundNode) != null)
            _popup.PopupPredicted(Loc.GetString(SurgeryGraph.Popup(foundNode)!, ("target", entity.Owner),
                ("user", performer), ("used", used == null ? Loc.GetString("surgery-null-used") : used)), entity.Owner, performer);
        // hands/pawns uh...
    }
}
