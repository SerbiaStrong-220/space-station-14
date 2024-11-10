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

    protected void ChangeSurgeryNode(Entity<OnSurgeryComponent> entity, string targetNode, EntityUid? performer = null)
    {
        var surgeryProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);
        ChangeSurgeryNode(entity, performer, targetNode, surgeryProto);
    }

    protected void StartSurgeryNode(Entity<OnSurgeryComponent> entity, EntityUid? performer = null)
    {
        var surgeryProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);
        ChangeSurgeryNode(entity, performer, surgeryProto.Start, surgeryProto);
    }

    // think of making struct which contains User and Used and etc. Pass it through ref to make locales richer.
    protected void ChangeSurgeryNode(Entity<OnSurgeryComponent> entity, EntityUid? performer, string targetNode, SurgeryGraphPrototype surgeryGraph)
    {
        if (!surgeryGraph.TryGetNode(targetNode, out var foundNode))
        {
            Log.Error($"No start node on graph {entity.Comp.SurgeryGraphProtoId} with name {targetNode}");
            return;
        }
        entity.Comp.CurrentNode = foundNode;

        _popup.PopupPredicted(SurgeryGraph.Popup(foundNode), entity.Owner, performer);
    }
}
