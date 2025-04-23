// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Audio;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;

namespace Content.Server.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    // TODO
    protected bool IsValidTarget(EntityUid uid, out string? reasonLocPath)
    {
        reasonLocPath = null;
        if (!HasComp<MobStateComponent>(uid)
            || !HasComp<SurgableComponent>(uid))
            return false;

        if (!_buckle.IsBuckled(uid))
        {
            reasonLocPath = "surgery-invalid-target-buckle";
            return false;
        }

        return true;
    }
    // TODO
    protected bool IsValidPerformer(EntityUid uid)
    {
        return true;
    }

    protected virtual void ProceedToNextStep(Entity<OnSurgeryComponent> entity, EntityUid user, EntityUid? used, SurgeryGraphEdge chosenEdge)
    {
        ChangeSurgeryNode(entity, chosenEdge.Target, user, used);

        _audio.PlayPredicted(SurgeryGraph.GetSoundSpecifier(chosenEdge), entity.Owner, user,
                        AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        if (OperationEnded(entity))
            EndOperation(entity);
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
    }

    protected bool OperationEnded(Entity<OnSurgeryComponent> entity)
    {
        var surgeryProto = _prototype.Index(entity.Comp.SurgeryGraphProtoId);

        if (entity.Comp.CurrentNode != surgeryProto.GetEndNode().Name)
            return false;

        return true;
    }


    protected bool OperationCanBeEnded(Entity<OnSurgeryComponent?> entity)
    {
        var (uid, comp) = entity;
        if (!Resolve(uid, ref comp))
            return false;

        var surgeryProto = _prototype.Index(comp.SurgeryGraphProtoId);

        var isStartNode = comp.CurrentNode == surgeryProto.Start;

        return isStartNode || OperationEnded((uid, comp));
    }

    protected void EndOperation(EntityUid entity)
    {
        RemComp<OnSurgeryComponent>(entity);
    }
}
