// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;

    private readonly LocId _cantStartUndefinedSurgery = "cant-start-surgery-while-on-surgery";
    private readonly LocId _cantStartSurgeryWhileOneOngoing = "cant-start-surgery-while-on-surgery";

    /// <summary>
    /// This fat method handles allowing to start surgery and ability to start surgery on best possible target (when <paramref name="target"/> is null)
    /// </summary>
    /// <param name="performer"> who started surgery </param>
    /// <param name="surgeryGraph"> what surgery we want to start </param>
    /// <param name="target"> whom we starting surgery or best possible candidate if null </param>
    /// <param name="used"> what we used to start surgery </param>
    /// <param name="reason"> not null when we cant start surgery </param>
    /// <returns> bool lol </returns>
    public bool CanStartSurgery(EntityUid performer, SurgeryGraphPrototype surgeryGraph, EntityUid? target, EntityUid? used, [NotNullWhen(false)] out string? reason)
    {
        reason = null;
        if (HasComp<OnSurgeryComponent>(target))
        {
            reason = Loc.GetString(_cantStartSurgeryWhileOneOngoing);
            return reason is null;
        }

        foreach (var requirement in surgeryGraph.Requirements)
        {
            var requirementTarget = ResolveRequirementSubject(requirement, performer, target, used);

            if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                continue;

            reason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);
            return false;
        }

        return true;
    }

    /// <inheritdoc cref="CanStartSurgery"/>
    public bool CanStartSurgery(EntityUid performer, ProtoId<SurgeryGraphPrototype> surgeryId, EntityUid? target, EntityUid? used, [NotNullWhen(false)] out string? reason)
    {
        if (!_prototype.Resolve(surgeryId, out var surgeryGraph))
        {
            reason = Loc.GetString(_cantStartUndefinedSurgery);
            return false;
        }

        return CanStartSurgery(performer, surgeryGraph, target, used, out reason);
    }

    public EntityUid? ResolveRequirementSubject(SurgeryGraphRequirement requirement, EntityUid performer, EntityUid? target, EntityUid? used)
    {
        var requirementTarget = requirement.Subject switch
        {
            SurgeryGraphRequirementSubject.Target => target,
            SurgeryGraphRequirementSubject.Performer => performer,
            SurgeryGraphRequirementSubject.Used => used,
            SurgeryGraphRequirementSubject.Container => target is null || !_sharedContainer.IsEntityInContainer(target.Value) ? null : Transform(target.Value).ParentUid,
            _ => EntityUid.Invalid
        };

        if (requirementTarget == EntityUid.Invalid)
        {
            Log.Error($"Got undefined entity uid to pick from {nameof(SurgeryGraphRequirementSubject)} with enum value {requirement.Subject}!");
            return performer;
        }

        return requirementTarget;
    }

    protected virtual void ProceedToNextStep(Entity<OnSurgeryComponent> entity, EntityUid user, EntityUid? used, SurgeryGraphEdge chosenEdge)
    {
        ChangeSurgeryNode(entity, chosenEdge.Target, user, used);

        _audio.PlayPredicted(SurgeryGraph.GetSoundSpecifier(chosenEdge), entity.Owner, user,
                        SurgeryGraph.GetSoundSpecifier(chosenEdge)?.Params.WithVolume(1f));

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
        if (SurgeryGraph.Popup(foundNode) == null)
            return;

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
