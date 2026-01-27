// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    private readonly LocId _cantStartUndefinedSurgery = "cant-start-surgery-while-on-surgery";
    private readonly LocId _cantStartSurgeryWhileOneOngoing = "cant-start-surgery-while-on-surgery";

    public bool OperationCanBeEnded(Entity<SurgeryPatientComponent?> entity, ProtoId<SurgeryGraphPrototype> surgeryGraph)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!entity.Comp.OngoingSurgeries.TryGetValue(surgeryGraph, out var currentNode))
            return false;

        if (!_prototype.Resolve(surgeryGraph, out var surgeryProto))
            return false;

        if (currentNode == surgeryProto.Start)
            return true;

        return currentNode == surgeryProto.GetEndNode().Name;
    }

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
        if (target is not null
            && TryComp<SurgeryPatientComponent>(target, out var surgeryPatient)
            && surgeryPatient.OngoingSurgeries.ContainsKey(surgeryGraph.ID))
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
            SurgeryGraphRequirementSubject.Strap => GetStrapSubject(target),
            _ => EntityUid.Invalid
        };

        return requirementTarget;
    }

    private EntityUid? GetStrapSubject(EntityUid? target)
    {
        if (target is null)
            return null;

        if (!TryComp<BuckleComponent>(target.Value, out var buckleComponent) || !buckleComponent.Buckled)
            return EntityUid.Invalid;

        return buckleComponent.BuckledTo;
    }

    protected virtual void ProceedToNextStep(Entity<SurgeryPatientComponent> entity, EntityUid user, EntityUid? used, ProtoId<SurgeryGraphPrototype> surgeryGraph, SurgeryGraphEdge chosenEdge)
    {
        ChangeSurgeryNode(entity, surgeryGraph, chosenEdge.Target, user, used);

        _audio.PlayPredicted(SurgeryGraph.GetSoundSpecifier(chosenEdge), entity.Owner, user);

        if (OperationCanBeEnded(entity!, surgeryGraph))
            EndOperation(entity!, surgeryGraph, user);
    }

    protected void ChangeSurgeryNode(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> surgeryGraph, string targetNode, EntityUid performer, EntityUid? used)
    {
        var surgeryProto = _prototype.Index(surgeryGraph);
        ChangeSurgeryNode(entity, performer, used, surgeryProto, targetNode);
    }

    protected void StartSurgeryNode(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> surgeryGraph, EntityUid performer, EntityUid? used)
    {
        // here hardcoded one operation at a time (TODO: maybe add radial menu for possible interactions?)
        if (entity.Comp.OngoingSurgeries.Count > 0)
        {
            Log.Fatal("Skill issue lol");
            return;
        }

        if (!_prototype.Resolve(surgeryGraph, out var surgeryProto))
            return;

        ChangeSurgeryNode(entity, performer, used, surgeryProto, surgeryProto.Start);
    }

    protected void ChangeSurgeryNode(Entity<SurgeryPatientComponent> entity, EntityUid performer, EntityUid? used, SurgeryGraphPrototype surgeryGraph, string targetNode)
    {
        if (!surgeryGraph.TryGetNode(targetNode, out var foundNode))
        {
            Log.Fatal($"No node on graph {surgeryGraph.ID} with name {targetNode}");
            return;
        }

        entity.Comp.OngoingSurgeries[surgeryGraph.ID] = foundNode.Name;

        if (SurgeryGraph.Popup(foundNode) is null)
            return;

        _popup.PopupPredicted(Loc.GetString(SurgeryGraph.Popup(foundNode)!, ("target", entity.Owner),
            ("user", performer), ("used", used == null ? Loc.GetString("surgery-null-used") : used)), entity.Owner, performer);
    }

    protected void EndOperation(Entity<SurgeryPatientComponent?> entity, ProtoId<SurgeryGraphPrototype> surgeryGraphId, EntityUid user)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        _adminLogManager.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):user} ended surgery {surgeryGraphId} on {ToPrettyString(entity):target}");

        entity.Comp.OngoingSurgeries.Remove(surgeryGraphId);
    }
}
