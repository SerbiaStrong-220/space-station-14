// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Conditions;

[UsedImplicitly]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class TotalDamageCondition : ISurgeryGraphCondition
{
    [DataField(required: true)]
    public FlippingCondition<FixedPoint2> FlippingCondition;

    public bool Condition(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<DamageableComponent>(targetUid, out var damageableComponent))
            return false;

        return FlippingCondition.IsPassed((x) => damageableComponent.TotalDamage > x,
                                            (x) => damageableComponent.TotalDamage < x);
    }

    public string ConditionDescription()
    {
        return Loc.GetString($"surgery-condition-total-damage-{FlippingCondition.Value}-condition-type-{FlippingCondition.ConditionType.ToString().ToLower()}");
    }
}
