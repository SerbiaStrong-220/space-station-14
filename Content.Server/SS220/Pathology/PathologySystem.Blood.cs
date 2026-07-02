// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class PathologySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    // reused per call so re-stamping blood doesn't allocate a set each time virus contents change
    private readonly HashSet<string> _bloodPrototypes = new();

    protected override void OnVirusContentsChanged(EntityUid uid)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var blood) || !TryComp<PathologyHolderComponent>(uid, out var holder))
            return;

        VirusData? virusData = null;
        if (holder.ActiveViruses.Count > 0)
        {
            var viruses = new List<VirusInstance>();
            foreach (var virus in holder.ActiveViruses.Values)
            {
                var clone = virus.Clone();
                clone.SymptomStages.Clear();
                foreach (var symptom in clone.Symptoms)
                {
                    if (holder.ActivePathologies.TryGetValue(symptom, out var data))
                        clone.SymptomStages[symptom] = data.Level;
                }

                viruses.Add(clone);
            }

            virusData = new VirusData { Viruses = viruses };
        }

        _bloodPrototypes.Clear();
        foreach (var quantity in blood.BloodReferenceSolution.Contents)
            _bloodPrototypes.Add(quantity.Reagent.Prototype);

        RestampBlood(blood.BloodReferenceSolution, _bloodPrototypes, virusData);

        if (_solutionContainer.ResolveSolution(uid, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodSolution))
            RestampBlood(bloodSolution, _bloodPrototypes, virusData);
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

                // keep any non-virus data (DNA etc.), drop old virus stamps. allocate only if there's any
                if (quantity.Reagent.Data != null)
                {
                    foreach (var entry in quantity.Reagent.Data)
                    {
                        if (entry is VirusData)
                            continue;

                        baseData ??= new List<ReagentData>();
                        baseData.Add(entry);
                    }
                }

                break;
            }

            solution.RemoveReagent(new ReagentId(prototype, null), total, ignoreReagentData: true);

            var newData = baseData ?? new List<ReagentData>();
            if (virusData != null)
                newData.Add(virusData);

            solution.AddReagent(new ReagentId(prototype, newData.Count > 0 ? newData : null), total);
        }
    }
}
