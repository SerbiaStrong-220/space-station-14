// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Components;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class SurgeryToolRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public SurgeryToolType SurgeryTool = SurgeryToolType.Invalid;

    public override bool SatisfiesRequirements(EntityUid? uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<SurgeryToolComponent>(uid, out var surgeryTool))
            return false;

        return surgeryTool.ToolType == SurgeryTool;
    }

    public override bool MeetRequirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(uid, entityManager))
            return false;

        return true;
    }
}
