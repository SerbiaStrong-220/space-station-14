// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Systems;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class SurgeryToolRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public SurgeryToolType SurgeryTool = SurgeryToolType.Invalid;

    protected override bool Requirement(EntityUid? uid, EntityUid user, IEntityManager entityManager)
    {
        if (uid is not { } used)
            return false;

        if (!entityManager.TryGetComponent<SurgeryToolComponent>(used, out var surgeryTool))
            return entityManager.System<SharedSurgerySystem>().IsImprovisedTool(used, SurgeryTool);

        return surgeryTool.ToolType == SurgeryTool || surgeryTool.AdditionalToolTypes.Contains(SurgeryTool);
    }
}
