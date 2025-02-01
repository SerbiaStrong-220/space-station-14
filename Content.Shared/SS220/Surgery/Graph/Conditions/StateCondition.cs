// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Surgery.Graph.Conditions;

[UsedImplicitly]
[Serializable]
[DataDefinition]
public sealed partial class StateCondition : IAbstractSurgeryGraphAvailabilityCondition
{
    [DataField(required: true)]
    public FlippingCondition<List<MobState>> FlippingCondition;

    [DataField]
    public string FailReasonPath = "surgery-availability-condition-state";

    [DataField]
    public string BaseFailReasonPath = "surgery-availability-condition-base-fail";


    public bool Condition(EntityUid uid, IEntityManager entityManager, [NotNullWhen(false)] out string? reason)
    {
        if (!entityManager.TryGetComponent<MobStateComponent>(uid, out var mobStateComponent))
        {
            reason = Loc.GetString(BaseFailReasonPath);
            return false;
        }

        reason = Loc.GetString($"{FailReasonPath}-{FlippingCondition.ConditionType.ToString().ToLower()}");

        return FlippingCondition.IsPassed((x) => x.Contains(mobStateComponent.CurrentState),
                                            (x) => !x.Contains(mobStateComponent.CurrentState));
    }
}
