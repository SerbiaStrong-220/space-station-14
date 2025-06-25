// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Surgery.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Action;

[DataDefinition]
public sealed partial class ConsumeReagentInHandAction : ISurgeryGraphAction
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> ReagentId;

    [DataField(required: true)]
    public FixedPoint2 ConsumedAmount;

    [DataField]
    public string SolutionName = "drink";

    public void PerformAction(EntityUid uid, EntityUid userUid, EntityUid? used, IEntityManager entityManager)
    {
        var sharedSolutionContainerSystem = entityManager.System<SharedSolutionContainerSystem>();

        if (!entityManager.TryGetComponent<HandsComponent>(userUid, out var handsComponent))
        {
            entityManager.System<SharedSurgerySystem>().Log.Error($"Trying to perform action {nameof(ConsumeReagentInHandAction)} but performer have no hand");
            return;
        }

        foreach (var (_, hand) in handsComponent.Hands)
        {
            if (hand.HeldEntity is null)
                continue;

            Entity<SolutionComponent>? solutionEntity = null;
            if (!sharedSolutionContainerSystem.ResolveSolution(hand.HeldEntity.Value, SolutionName, ref solutionEntity, out _))
                continue;

            if (sharedSolutionContainerSystem.RemoveReagent(solutionEntity.Value, ReagentId, ConsumedAmount))
                return;
        }

        entityManager.System<SharedSurgerySystem>().Log.Error($"Trying to perform action {nameof(ConsumeReagentInHandAction)} but cant find any solution to drain reagent from");
    }
}
