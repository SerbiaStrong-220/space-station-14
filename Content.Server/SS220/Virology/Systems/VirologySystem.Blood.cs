// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Virology;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    // reused so re-stamping doesn't allocate a set each change
    private readonly HashSet<string> _bloodPrototypes = [];

    private void InitializeBlood()
    {
        SubscribeLocalEvent<VirusHolderComponent, VirusContentsChangedEvent>(OnContentsChanged);
    }

    private void OnContentsChanged(Entity<VirusHolderComponent> ent, ref VirusContentsChangedEvent args)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var blood))
            return;

        var virusData = BuildData(ent);

        _bloodPrototypes.Clear();
        foreach (var quantity in blood.BloodReferenceSolution.Contents)
            _bloodPrototypes.Add(quantity.Reagent.Prototype);

        RestampBlood(blood.BloodReferenceSolution, _bloodPrototypes, virusData);

        if (_solutionContainer.ResolveSolution(ent.Owner, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodSolution))
            RestampBlood(bloodSolution, _bloodPrototypes, virusData);
    }

    private VirusData? BuildData(Entity<VirusHolderComponent> ent)
    {
        if (ent.Comp.Viruses.Count == 0)
            return null;

        var descriptors = new List<VirusDescriptor>();
        foreach (var strain in EnumerateStrains(ent.Comp))
            descriptors.Add(ToDescriptor(strain));

        return descriptors.Count > 0 ? new VirusData { Viruses = descriptors } : null;
    }

    private static void RestampBlood(Solution solution, HashSet<string> bloodPrototypes, VirusData? virusData)
    {
        foreach (var prototype in bloodPrototypes)
        {
            var total = solution.GetTotalPrototypeQuantity(prototype);
            if (total <= FixedPoint2.Zero)
                continue;

            List<ReagentData>? baseData = null;
            foreach (var quantity in solution.Contents)
            {
                if (quantity.Reagent.Prototype != prototype)
                    continue;

                // keep any non-virus data (DNA), drop old virus stamps
                if (quantity.Reagent.Data != null)
                {
                    foreach (var entry in quantity.Reagent.Data)
                    {
                        if (entry is VirusData)
                            continue;

                        baseData ??= [];
                        baseData.Add(entry);
                    }
                }

                break;
            }

            solution.RemoveReagent(new ReagentId(prototype, null), total, ignoreReagentData: true);

            var newData = baseData ?? [];
            if (virusData != null)
                newData.Add(virusData);

            solution.AddReagent(new ReagentId(prototype, newData.Count > 0 ? newData : null), total);
        }
    }
}
