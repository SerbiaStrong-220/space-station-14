// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Conditions;

[UsedImplicitly]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class MobStateCondition : ISurgeryGraphCondition
{
    [DataField(required: true)]
    public FlippingCondition<List<MobState>> FlippingCondition;

    public bool Condition(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<MobStateComponent>(targetUid, out var mobStateComponent))
            return false;

        return FlippingCondition.IsPassed((x) => x.Contains(mobStateComponent.CurrentState),
                                            (x) => !x.Contains(mobStateComponent.CurrentState));
    }

    public string ConditionDescription()
    {
        StringBuilder stringBuilder = new();

        stringBuilder.Append(Loc.GetString($"surgery-condition-mob-state"));
        stringBuilder.AppendJoin(' ', FlippingCondition.Value);

        return stringBuilder.ToString();
    }
}
