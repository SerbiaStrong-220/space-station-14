// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class TotalDamageRequirement : SurgeryGraphRequirement
{
    [DataField]
    public ProtoId<DamageTypePrototype>? DamageType;

    [DataField(required: true)]
    public FixedPoint2 Damage;

    protected override bool Requirement(EntityUid? uid, IEntityManager entityManager)
    {
        var damageSystem = entityManager.System<DamageableSystem>();
        if (!entityManager.TryGetComponent<DamageableComponent>(uid, out var damageableComponent))
            return false;

        if (DamageType is not null)
            return damageSystem.GetAllDamage(uid.Value)[DamageType] > Damage;
        else
            return damageSystem.GetTotalDamage(uid.Value) > Damage;
    }
}
