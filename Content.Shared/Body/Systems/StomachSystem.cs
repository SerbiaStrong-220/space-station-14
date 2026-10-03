using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Body.Systems;

public sealed partial class StomachSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public const string DefaultSolutionName = "stomach";

    public bool CanTransferSolution(Entity<StomachComponent?, SolutionManagerComponent?> entity, Solution solution)
    {
        return Resolve(entity, ref entity.Comp1, logMissing: false)
            && _solutionContainerSystem.ResolveSolution((entity, entity.Comp2), DefaultSolutionName, ref entity.Comp1.Solution, out var stomachSolution)
            // TODO: For now no partial transfers. Potentially change by design
            && stomachSolution.CanAddSolution(solution);
    }

    public bool TryTransferSolution(Entity<StomachComponent?, SolutionManagerComponent?> entity, Solution solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false)
            || !_solutionContainerSystem.ResolveSolution((entity, entity.Comp2), DefaultSolutionName, ref entity.Comp1.Solution)
            || !CanTransferSolution(entity, solution))
            return false;

        _solutionContainerSystem.TryAddSolution(entity.Comp1.Solution.Value, solution);
        return true;
    }

    // SS220 remove reagent from stomach begin
    public bool TryRemoveReagent(
        EntityUid uid,
        ReagentQuantity reagent,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        if (!Resolve(uid, ref stomach, ref solutions)
            || !_solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution, out var solution))
            return false;

        solution.RemoveReagent(reagent);
        _solutionContainerSystem.UpdateChemicals(stomach.Solution.Value);

        return true;
    }
    // SS220 remove reagent from stomach end
}
