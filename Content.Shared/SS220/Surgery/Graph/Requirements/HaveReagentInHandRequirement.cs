// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Surgery.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class HaveReagentInHandRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> ReagentId;

    [DataField(required: true)]
    public FixedPoint2 ConsumedAmount;

    protected override bool Requirement(EntityUid? uid, EntityUid user, IEntityManager entityManager)
    {
        if (uid is null)
            return false;

        return TryFindSolution(uid.Value, entityManager, out _);
    }

    protected override void AfterRequirementMet(EntityUid? uid, IEntityManager entityManager)
    {
        if (uid is null)
            return;

        if (!TryFindSolution(uid.Value, entityManager, out var solution))
        {
            entityManager.System<SharedSurgerySystem>().Log.Error($"Trying to meet {nameof(HaveReagentInHandRequirement)} but cant find any solution to drain reagent from");
            return;
        }

        entityManager.System<SharedSolutionContainerSystem>().RemoveReagent(solution.Value, ReagentId, ConsumedAmount);
    }

    /// <summary>
    /// Finds a single held solution with enough reagent, so the check and the consumption always agree.
    /// </summary>
    private bool TryFindSolution(EntityUid holder, IEntityManager entityManager, [NotNullWhen(true)] out Entity<SolutionComponent>? found)
    {
        found = null;

        var handSystem = entityManager.System<SharedHandsSystem>();
        var solutionSystem = entityManager.System<SharedSolutionContainerSystem>();

        foreach (var heldItem in handSystem.EnumerateHeld(holder))
        {
            if (!entityManager.TryGetComponent<SolutionContainerManagerComponent>(heldItem, out var heldSolutionManager))
                continue;

            foreach (var solutionName in heldSolutionManager.Containers)
            {
                Entity<SolutionComponent>? solutionEntity = null;
                if (!solutionSystem.ResolveSolution(heldItem, solutionName, ref solutionEntity, out var solution))
                    continue;

                if (solution.GetTotalPrototypeQuantity(ReagentId) < ConsumedAmount)
                    continue;

                found = solutionEntity;
                return true;
            }
        }

        return false;
    }

    public override string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        if (!prototypeManager.Resolve(ReagentId, out var reagentPrototype))
            return string.Empty;

        return Loc.GetString(Description, ("reagent", reagentPrototype.LocalizedName), ("amount", ConsumedAmount));
    }

    public override string RequirementFailureReason(EntityUid? uid, IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return RequirementDescription(prototypeManager, entityManager);
    }
}
