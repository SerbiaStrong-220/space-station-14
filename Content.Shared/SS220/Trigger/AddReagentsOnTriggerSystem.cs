using Content.Shared.Charges.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Trigger;

namespace Content.Shared.SS220.Trigger;

public sealed class AddReagentsOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private const string BeakerSolution = "beaker";
    private const string ChemicalSolution = "chemicals";

    public override void Initialize()
    {
        SubscribeLocalEvent<AddReagentsOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<AddReagentsOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<SolutionContainerManagerComponent>(ent.Owner, out var solutionTriggerComp))
            return;

        if (!TryComp<SolutionContainerManagerComponent>(target, out var solutionUserComp))
            return;

        if (!_solution.TryGetSolution((ent.Owner, solutionTriggerComp), BeakerSolution, out var solutionTrigger))
            return;

        if (!_solution.TryGetSolution((target.Value, solutionUserComp), ChemicalSolution, out var solutionUser))
            return;

        var quantity = solutionTrigger.Value.Comp.Solution.MaxVolume;
        if (TryComp<LimitedChargesComponent>(ent, out var actionCharges))
        {
            quantity /= actionCharges.MaxCharges;
        }
        else if (TryComp<SubdermalImplantComponent>(ent, out var implant) &&
                 TryComp<LimitedChargesComponent>(implant.Action, out var limitedCharges))
        {
            quantity /= limitedCharges.MaxCharges;
        }

        _solution.TryTransferSolution(solutionUser.Value,
            solutionTrigger.Value.Comp.Solution,
            quantity);
    }
}
