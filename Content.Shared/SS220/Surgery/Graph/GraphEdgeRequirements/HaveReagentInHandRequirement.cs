// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Surgery.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.GraphEdgeRequirements;

[DataDefinition]
public sealed partial class HaveReagentInHandRequirement : SurgeryGraphEdgeRequirement
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> ReagentId;

    [DataField(required: true)]
    public FixedPoint2 ConsumedAmount;

    [DataField]
    public string SolutionName = "drink";

    public override bool SatisfiesRequirements(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        var handSystem = entityManager.System<SharedHandsSystem>();
        var solutionSystem = entityManager.System<SharedSolutionContainerSystem>();

        foreach (var heldItem in handSystem.EnumerateHeld(userUid))
        {
            if (solutionSystem.GetTotalPrototypeQuantity(heldItem, ReagentId) >= ConsumedAmount)
                return true;
        }

        return false;
    }

    public override bool MeetRequirement(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(targetUid, toolUid, userUid, entityManager))
            return false;

        var handSystem = entityManager.System<SharedHandsSystem>();
        var solutionSystem = entityManager.System<SharedSolutionContainerSystem>();

        foreach (var heldItem in handSystem.EnumerateHeld(userUid))
        {
            Entity<SolutionComponent>? solutionEntity = null;
            if (!solutionSystem.ResolveSolution(heldItem, SolutionName, ref solutionEntity, out _))
                continue;

            if (!(solutionSystem.GetTotalPrototypeQuantity(heldItem, ReagentId) >= ConsumedAmount))
                continue;

            solutionSystem.RemoveReagent(solutionEntity.Value, ReagentId, ConsumedAmount);
            return true;
        }

        entityManager.System<SharedSurgerySystem>().Log.Error($"Trying to meet {nameof(HaveReagentInHandRequirement)} but cant find any solution to drain reagent from");

        return false;
    }

    public override string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString($"surgery-requirement-reagent-in-hand");
    }
}
