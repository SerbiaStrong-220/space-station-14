// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Ui;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.Commands.Values;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    public SurgeryEdgeSelectorEdgesState MakeSelectorState(Entity<SurgeryPatientComponent> entity, EntityUid? used, EntityUid user)
    {
        var edgesInfoList = new List<EdgeSelectInfo>();

        foreach (var (surgeryId, node) in entity.Comp.OngoingSurgeries)
        {
            if (!CanPerformAnyEdgeInSurgery(entity, surgeryId, used, user))
                continue;

            var meetRequirement = true;
            foreach (var edge in GetEdges(surgeryId, node))
            {
                foreach (var requirement in SurgeryGraph.GetRequirements(edge))
                {
                    var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

                    if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                        continue;

                    meetRequirement = false;
                    break;
                }

                edgesInfoList.Add(new(edge.Target, surgeryId, edge.EdgeTooltip, meetRequirement, SurgeryGraph.EdgeIcon(edge)));
            }
        }

        return new SurgeryEdgeSelectorEdgesState { Infos = edgesInfoList };
    }

    private IEnumerable<SurgeryGraphEdge> GetEdges(ProtoId<SurgeryGraphPrototype> surgeryGraphId, string node)
    {
        if (!_prototype.Resolve(surgeryGraphId, out var graphProto))
            yield break;

        if (!graphProto.TryGetNode(node, out var currentNode))
        {
            Log.Fatal($"Current node has incorrect value {node} for graph proto {surgeryGraphId}");
            yield break;
        }

        foreach (var edge in currentNode.Edges)
        {
            yield return edge;
        }
    }

    private void PopupSurgeryGraphFailures(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> surgeryGraphId, EntityUid? used, EntityUid user)
    {
        if (!_prototype.Resolve(surgeryGraphId, out var graphProto))
            return;

        foreach (var requirement in graphProto.Requirements)
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

            if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                continue;

            var reason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);

            _popup.PopupClient(reason, user, user);
        }
    }

    private SurgeryGraphEdge? GetEdgeTargeting(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> surgeryGraphId, string targetNode)
    {
        if (!entity.Comp.OngoingSurgeries.TryGetValue(surgeryGraphId, out var currentNode))
        {
            Log.Error($"Tried to get edge targeting node {targetNode} in surgery {surgeryGraphId}, but entity doesn't have this surgery ongoing!");
            return null;
        }

        if (!_prototype.Resolve(surgeryGraphId, out var graphProto))
            return null;

        if (!graphProto.TryGetNode(currentNode, out var currentSurgeryNode))
        {
            Log.Fatal($"Current node has incorrect value {currentNode} for graph proto {surgeryGraphId}");
            return null;
        }

        return currentSurgeryNode.Edges.FirstOrDefault<SurgeryGraphEdge?>(x => x?.Target == targetNode);
    }

    public bool CanPerformAnyEdgeInSurgery(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> graphProtoId, EntityUid? used, EntityUid user)
    {
        if (!_prototype.Resolve(graphProtoId, out var graphProto))
            return false;

        return CanPerformAnyEdgeInSurgery(entity, graphProto, used, user);
    }

    public bool CanPerformAnyEdgeInSurgery(Entity<SurgeryPatientComponent> entity, SurgeryGraphPrototype graphProto, EntityUid? used, EntityUid user)
    {
        foreach (var requirement in graphProto.Requirements)
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

            if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                continue;

            return false;
        }

        return true;
    }

    public PerformSurgeryEdgeInfo GetPerformSurgeryEdgeInfo(Entity<SurgeryPatientComponent> entity, SurgeryGraphEdge edge, EntityUid? used, EntityUid user)
    {
        foreach (var requirement in SurgeryGraph.GetVisibilityRequirements(edge))
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

            if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                continue;

            return new PerformSurgeryEdgeInfo(edge.Target, false, null);
        }

        foreach (var requirement in SurgeryGraph.GetRequirements(edge))
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

            if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                continue;

            var reason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);

            return new PerformSurgeryEdgeInfo(edge.Target, true, reason);
        }

        return new PerformSurgeryEdgeInfo(edge.Target, true, null);
    }
}

public readonly record struct PerformSurgeryEdgeInfo(string TargetNode, bool Visible, string? FailureReason);
