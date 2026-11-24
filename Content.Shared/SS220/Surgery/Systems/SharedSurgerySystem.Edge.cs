// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Ui;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    /// <summary>
    /// Used to drop obvious message like "use scalpel to make incision" while player sees scalpel on UI and edge's named incision
    /// </summary>
    private const int RequirementPriorityFailureMessageEdgeSelectorDrop = -1;

    public SurgeryEdgeSelectorEdgesState MakeSelectorState(Entity<SurgeryPatientComponent> entity, EntityUid? used, EntityUid user)
    {
        var edgesInfoList = new List<EdgeSelectInfo>();

        foreach (var (surgeryId, node) in entity.Comp.OngoingSurgeries)
        {
            if (!CanPerformAnyEdgeInSurgery(entity, surgeryId, used, user))
                continue;

            if (!TryGetSurgeryNode(surgeryId, node, out var currentNode))
                continue;

            foreach (var edge in currentNode.Edges)
            {
                // hidden edges (e.g. syndicate-only steps) must not be offered at all
                if (!IsEdgeVisible(entity, edge, used, user))
                    continue;

                var requirementPriority = RequirementPriorityFailureMessageEdgeSelectorDrop;
                string? failureReason = null;
                var meetRequirement = true;
                foreach (var requirement in SurgeryGraph.GetRequirements(edge))
                {
                    var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

                    if (requirement.SatisfiesRequirements(requirementTarget, user, used, EntityManager))
                        continue;

                    meetRequirement = false;

                    if (requirement.RequirementPriority > requirementPriority)
                    {
                        failureReason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);
                        requirementPriority = requirement.RequirementPriority;
                    }

                    break;
                }

                edgesInfoList.Add(new(edge.Id, edge.Target, surgeryId, edge.EdgeTooltip, meetRequirement, SurgeryGraph.EdgeIcon(edge), failureReason));
            }
        }

        return new SurgeryEdgeSelectorEdgesState { Infos = edgesInfoList, Used = GetNetEntity(used) };
    }

    private bool TryGetSurgeryNode(ProtoId<SurgeryGraphPrototype> surgeryGraphId, string node, [NotNullWhen(true)] out SurgeryGraphNode? foundNode)
    {
        foundNode = null;
        if (!_prototype.Resolve(surgeryGraphId, out var graphProto))
            return false;

        if (!graphProto.TryGetNode(node, out foundNode))
        {
            Log.Fatal($"Current node has incorrect value {node} for graph proto {surgeryGraphId}");
            return false;
        }

        return true;
    }

    private void PopupSurgeryGraphFailures(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> surgeryGraphId, EntityUid? used, EntityUid user)
    {
        if (!_prototype.Resolve(surgeryGraphId, out var graphProto))
            return;

        foreach (var requirement in graphProto.Requirements)
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

            if (requirement.SatisfiesRequirements(requirementTarget, user, used, EntityManager))
                continue;

            var reason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);

            _popup.PopupClient(reason, user, user);
        }
    }

    private SurgeryGraphEdge? GetEdgeTargeting(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> surgeryGraphId, string edgeId)
    {
        // the operation may be cancelled during the step or a client message may be late, that's not an error
        if (!entity.Comp.OngoingSurgeries.TryGetValue(surgeryGraphId, out var currentNode))
            return null;

        if (!TryGetSurgeryNode(surgeryGraphId, currentNode, out var node))
            return null;

        foreach (var edge in node.Edges)
        {
            if (edge.Id == edgeId)
                return edge;
        }

        return null;
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

            if (requirement.SatisfiesRequirements(requirementTarget, user, used, EntityManager))
                continue;

            return false;
        }

        return true;
    }

    public bool CanMeetRequirements(Entity<SurgeryPatientComponent> entity, SurgeryGraphEdge edge, EntityUid? used, EntityUid user)
    {
        return CheckRequirements(entity, edge, used, user, applyEffects: false);
    }

    public bool TryMeetRequirement(Entity<SurgeryPatientComponent> entity, SurgeryGraphEdge edge, EntityUid? used, EntityUid user)
    {
        return CheckRequirements(entity, edge, used, user, applyEffects: true);
    }

    private bool CheckRequirements(Entity<SurgeryPatientComponent> entity, SurgeryGraphEdge edge, EntityUid? used, EntityUid user, bool applyEffects)
    {
        foreach (var requirement in SurgeryGraph.GetVisibilityRequirements(edge))
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);
            var satisfied = applyEffects
                ? requirement.MeetRequirement(requirementTarget, user, used, EntityManager)
                : requirement.SatisfiesRequirements(requirementTarget, user, used, EntityManager);

            if (satisfied)
                continue;

            return false;
        }

        foreach (var requirement in SurgeryGraph.GetRequirements(edge))
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);
            var satisfied = applyEffects
                ? requirement.MeetRequirement(requirementTarget, user, used, EntityManager)
                : requirement.SatisfiesRequirements(requirementTarget, user, used, EntityManager);

            if (satisfied)
                continue;

            var reason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);
            _popup.PopupClient(reason, user);

            return false;
        }

        return true;
    }

    public PerformSurgeryEdgeInfo GetPerformSurgeryEdgeInfo(Entity<SurgeryPatientComponent> entity, SurgeryGraphEdge edge, EntityUid? used, EntityUid user)
    {
        if (!IsEdgeVisible(entity, edge, used, user))
            return new PerformSurgeryEdgeInfo(edge.Target, false, null);

        foreach (var requirement in SurgeryGraph.GetRequirements(edge))
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

            if (requirement.SatisfiesRequirements(requirementTarget, user, used, EntityManager))
                continue;

            var reason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);

            return new PerformSurgeryEdgeInfo(edge.Target, true, reason);
        }

        return new PerformSurgeryEdgeInfo(edge.Target, true, null);
    }

    private bool IsEdgeVisible(Entity<SurgeryPatientComponent> entity, SurgeryGraphEdge edge, EntityUid? used, EntityUid user)
    {
        foreach (var requirement in SurgeryGraph.GetVisibilityRequirements(edge))
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

            if (!requirement.SatisfiesRequirements(requirementTarget, user, used, EntityManager))
                return false;
        }

        return true;
    }
}

public readonly record struct PerformSurgeryEdgeInfo(string TargetNode, bool Visible, string? FailureReason);
