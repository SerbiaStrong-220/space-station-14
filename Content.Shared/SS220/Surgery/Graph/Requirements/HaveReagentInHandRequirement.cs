// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Components;
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

    [DataField]
    public string SolutionName = "drink";

    public override bool SatisfiesRequirements(EntityUid? uid, IEntityManager entityManager)
    {
        if (uid is null)
            return false;

        var handSystem = entityManager.System<SharedHandsSystem>();
        var solutionSystem = entityManager.System<SharedSolutionContainerSystem>();

        foreach (var heldItem in handSystem.EnumerateHeld(uid.Value))
        {
            if (solutionSystem.GetTotalPrototypeQuantity(heldItem, ReagentId) >= ConsumedAmount)
                return true;
        }

        return false;
    }

    public override bool MeetRequirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (!base.MeetRequirement(uid, entityManager))
            return false;

        // whatever makes compiler happy
        if (uid is null)
            return false;

        var handSystem = entityManager.System<SharedHandsSystem>();
        var solutionSystem = entityManager.System<SharedSolutionContainerSystem>();

        foreach (var heldItem in handSystem.EnumerateHeld(uid.Value))
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
        if (!prototypeManager.Resolve(ReagentId, out var reagentPrototype))
            return string.Empty;

        return Loc.GetString(Description, ("reagent", reagentPrototype.LocalizedName), ("amount", ConsumedAmount));
    }

    public override string RequirementFailureReason(EntityUid? uid, IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return RequirementDescription(prototypeManager, entityManager);
    }
}
