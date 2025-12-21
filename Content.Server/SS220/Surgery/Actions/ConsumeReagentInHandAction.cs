// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Surgery.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
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
        var handSystem = entityManager.System<SharedHandsSystem>();
        var solutionSystem = entityManager.System<SharedSolutionContainerSystem>();

        foreach (var heldItem in handSystem.EnumerateHeld(userUid))
        {
            Entity<SolutionComponent>? solutionEntity = null;
            if (!solutionSystem.ResolveSolution(heldItem, SolutionName, ref solutionEntity, out _))
                continue;

            // Reagent amount check in condition, so without it
            solutionSystem.RemoveReagent(solutionEntity.Value, ReagentId, ConsumedAmount);
        }

        entityManager.System<SharedSurgerySystem>().Log.Error($"Trying to perform action {nameof(ConsumeReagentInHandAction)} but cant find any solution to drain reagent from");
    }
}
