// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.SS220.Surgery.Graph;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Conditions;

[UsedImplicitly]
[Serializable]
[DataDefinition]
public sealed partial class HaveReagentInHandCondition : ISurgeryGraphCondition
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> ReagentId;

    [DataField(required: true)]
    public FixedPoint2 ConsumedAmount;

    public bool Condition(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HandsComponent>(userUid, out var handsComponent))
            return false;

        foreach (var (_, hand) in handsComponent.Hands)
        {
            if (hand.HeldEntity is null)
                continue;

            // This can be a problem cause it gets all solution containers
            if (entityManager.System<SharedSolutionContainerSystem>().GetTotalPrototypeQuantity(hand.HeldEntity.Value, ReagentId) >= ConsumedAmount)
                return true;
        }

        return false;
    }

    public string ConditionDescription()
    {
        return Loc.GetString($"surgery-condition-reagent-in-hand", ("reagent", Loc.GetString(ReagentId)));
    }
}
