// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.Virology.Conditions;

public sealed partial class VirusReagentProgressCondition : VirusProgressCondition
{
    /// <summary>Dose required to advance.</summary>
    [DataField]
    public FixedPoint2 Amount = 5;

    protected override bool Condition(in VirusProgressArgs args)
    {
        if (args.IsClient)
            return false;

        if (args.Symptom.Accelerant is not { } accelerant)
            return false;

        if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.Carrier, out var blood))
            return false;

        var solutionContainer = args.EntityManager.System<SharedSolutionContainerSystem>();
        if (!solutionContainer.ResolveSolution(args.Carrier, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodSolution)
            || blood.BloodSolution is not { } bloodSolnEntity)
            return false;

        if (bloodSolution.GetTotalPrototypeQuantity(accelerant) < Amount)
            return false;

        solutionContainer.RemoveReagent(bloodSolnEntity, accelerant, Amount);

        var ev = new VirusDoseAbsorbedEvent(args.Symptom);
        args.EntityManager.EventBus.RaiseLocalEvent(args.Virus, ref ev);
        return true;
    }
}
