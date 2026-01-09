// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.GraphEdgeRequirements;

[DataDefinition]
public sealed partial class SurgeryToolRequirement : SurgeryGraphEdgeRequirement
{
    [DataField(required: true)]
    public SurgeryToolType SurgeryTool = SurgeryToolType.Invalid;

    public override bool SatisfiesRequirements(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<SurgeryToolComponent>(toolUid, out var surgeryTool)
            || surgeryTool.ToolType != SurgeryTool)
            return false;

        return true;
    }

    public override bool MeetRequirement(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(targetUid, toolUid, userUid, entityManager))
            return false;

        return true;
    }

    public override string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString($"surgery-requirement-surgery-tool-{SurgeryTool.ToString().ToLower()}");
    }
}
