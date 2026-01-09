// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class TotalDamageRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public FixedPoint2 Damage;

    [DataField]
    public bool Invert = false;

    public override bool SatisfiesRequirements(EntityUid? uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<DamageableComponent>(uid, out var damageableComponent))
            return false;

        return !Invert && damageableComponent.TotalDamage > Damage;
    }

    public override bool MeetRequirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(uid, entityManager))
            return false;

        return true;
    }
}
