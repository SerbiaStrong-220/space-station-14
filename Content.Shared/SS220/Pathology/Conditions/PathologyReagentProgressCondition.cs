// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.Pathology.Conditions;

public sealed partial class PathologyReagentProgressCondition : PathologyProgressCondition
{
    /// <summary>Dose required to advance, spent on each advance.</summary>
    [DataField]
    public FixedPoint2 Amount = 5;

    protected override bool Condition(in PathologyEffectArgs args)
    {
        if (args.IsClient)
            return false;

        if (args.Data.Accelerant is not { } accelerant)
            return false;

        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.Target, out var blood))
            return false;

        var solutionContainer = args.EntityManager.System<SharedSolutionContainerSystem>();
        if (!solutionContainer.ResolveSolution(args.Target, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodSolution)
            || blood.BloodSolution is not { } bloodSolnEntity)
            return false;

        if (bloodSolution.GetTotalPrototypeQuantity(accelerant) < Amount)
            return false;

        solutionContainer.RemoveReagent(bloodSolnEntity, accelerant, Amount);
        return true;
    }
}
