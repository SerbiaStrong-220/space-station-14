// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.GraphEdgeRequirements;

[DataDefinition]
public sealed partial class MobStateRequirement : SurgeryGraphEdgeRequirement
{
    [DataField(required: true)]
    public HashSet<MobState> States;

    [DataField]
    public bool Invert = false;

    public override bool SatisfiesRequirements(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<MobStateComponent>(targetUid, out var mobStateComponent))
            return false;

        return !Invert && States.Contains(mobStateComponent.CurrentState);
    }

    public override bool MeetRequirement(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(targetUid, toolUid, userUid, entityManager))
            return false;

        return true;
    }

    public override string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString($"surgery-requirement-mob-state");
    }
}
