// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;

namespace Content.Shared.SS220.Surgery.Graph.GraphEdgeRequirements;

[DataDefinition]
public sealed partial class TotalDamageRequirement : SurgeryGraphEdgeRequirement
{
    [DataField(required: true)]
    public FixedPoint2 Damage;

    [DataField]
    public bool Invert = false;

    public override bool SatisfiesRequirements(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<DamageableComponent>(targetUid, out var damageableComponent))
            return false;

        return !Invert && damageableComponent.TotalDamage > Damage;
    }

    public override bool MeetRequirement(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(targetUid, toolUid, userUid, entityManager))
            return false;

        return true;
    }

    public override string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString($"surgery-requirement-total-damage");
    }
}
