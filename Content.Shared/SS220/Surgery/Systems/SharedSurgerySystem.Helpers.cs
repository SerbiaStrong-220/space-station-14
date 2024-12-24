// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Audio;
using Content.Shared.SS220.MindSlave;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;

namespace Content.Server.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    protected bool IsValidTarget(EntityUid uid, out string? reasonLocPath)
    {
        reasonLocPath = null;
        if (HasComp<OnSurgeryComponent>(uid)
            || !HasComp<MindSlaveComponent>(uid) // for now only for slaves
            || !HasComp<SurgableComponent>(uid))
            return false;

        if (!_buckleSystem.IsBuckled(uid))
        {
            reasonLocPath = "surgery-invalid-target-buckle";
            return false;
        }

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

        if (entity.Comp.CurrentNode != surgeryProto.GetEndNode().Name)
            return false;

        return true;
    }

    protected virtual void ProceedToNextStep(Entity<OnSurgeryComponent> entity, EntityUid user, EntityUid? used, SurgeryGraphEdge chosenEdge)
    {
        ChangeSurgeryNode(entity, chosenEdge.Target, user, used);

        _audio.PlayPredicted(SurgeryGraph.GetSoundSpecifier(chosenEdge), entity.Owner, user,
                        AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        if (OperationEnded(entity))
            RemComp<OnSurgeryComponent>(entity.Owner);
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

    protected void ChangeSurgeryNode(Entity<OnSurgeryComponent> entity, EntityUid performer, EntityUid? used, string targetNode, SurgeryGraphPrototype surgeryGraph)
    {
        if (!surgeryGraph.TryGetNode(targetNode, out var foundNode))
        {
            Log.Error($"No start node on graph {entity.Comp.SurgeryGraphProtoId} with name {targetNode}");
            return;
        }

        entity.Comp.CurrentNode = foundNode.Name;
        if (SurgeryGraph.Popup(foundNode) != null)
            _popup.PopupPredicted(Loc.GetString(SurgeryGraph.Popup(foundNode)!, ("target", entity.Owner),
                ("user", performer), ("used", used == null ? Loc.GetString("surgery-null-used") : used)), entity.Owner, performer);
        // hands/pawns uh...
    }
}
