// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Shared.SS220.Surgery.Graph.Conditions;

[UsedImplicitly]
[Serializable]
[DataDefinition]
public sealed partial class DamageAmountCondition : IAbstractSurgeryGraphAvailabilityCondition
{
    [DataField(required: true)]
    public FlippingCondition<FixedPoint2> FlippingCondition;

    [DataField]
    public string FailReasonPath = "surgery-availability-condition-damage-amount";

    [DataField]
    public string BaseFailReasonPath = "surgery-availability-condition-base-fail";

    public bool Condition(EntityUid uid, IEntityManager entityManager, [NotNullWhen(false)] out string? reason)
    {
        if (!entityManager.TryGetComponent<DamageableComponent>(uid, out var damageableComponent))
        {
            reason = Loc.GetString(BaseFailReasonPath);
            return false;
        }

        reason = Loc.GetString($"{FailReasonPath}-{FlippingCondition.ConditionType.ToString().ToLower()}");

        return FlippingCondition.IsPassed((x) => damageableComponent.TotalDamage > x,
                                            (x) => damageableComponent.TotalDamage < x);
    }

}

public enum CheckTypes
{
    More,
    Less
}
