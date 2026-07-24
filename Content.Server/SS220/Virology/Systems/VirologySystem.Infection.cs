// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.Virology;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem
{
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly TimeSpan SuppressDuration = TimeSpan.FromMinutes(15);
    private static readonly FixedPoint2 MinCureReagentAmount = 5;
    private static readonly FixedPoint2 MinVaccineAmount = 5;
    private readonly List<VirusBroadReagentPrototype> _broadReagents = [];
    private readonly Dictionary<ReagentId, FixedPoint2> _vaccineTotals = [];
    private readonly List<Solution> _bodySolutions = [];

    private void InitializeInfection()
    {
        BuildBroadReagents();
    }

    private void BuildBroadReagents()
    {
        _broadReagents.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<VirusBroadReagentPrototype>())
            _broadReagents.Add(proto);
    }

    private void TickInfection()
    {
        var query = EntityQueryEnumerator<VirusSusceptibleComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out _, out var blood))
        {
            if (!_solutionContainer.ResolveSolution(uid, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodSolution))
                continue;

            InfectFromBlood(uid, bloodSolution);

            if (_mobState.IsDead(uid))
                continue;

            if (!TryComp<VirusHolderComponent>(uid, out var holder))
                continue;

            CollectBodySolutions(uid, blood, bloodSolution);

            foreach (var broad in _broadReagents)
                ApplyBroadReagent((uid, holder), broad);

            SuppressCured((uid, holder));
            ApplyVaccines((uid, holder));
        }
    }

    private void InfectFromBlood(EntityUid uid, Solution blood)
    {
        if (!HasVirus(blood))
            return;

        foreach (var quantity in new List<ReagentQuantity>(blood.Contents))
            InfectFromReagent(uid, quantity.Reagent, bloodborne: true);
    }

    private void ApplyBroadReagent(Entity<VirusHolderComponent> ent, VirusBroadReagentPrototype broad)
    {
        if (ReagentInBody(broad.Reagent) < broad.Amount)
            return;

        foreach (var virus in GetStrains(ent))
        {
            if (broad.Action == VirusBroadAction.Cure)
                RemoveVirus(virus);
            else if (virus.Comp.SuppressedUntil == null)
                SuppressVirus(virus, SuppressDuration);
        }
    }

    private void SuppressCured(Entity<VirusHolderComponent> ent)
    {
        foreach (var virus in GetStrains(ent))
        {
            var comp = virus.Comp;
            if (comp.SuppressedUntil != null)
                continue;

            if (comp.Cure is not { Reagents.Count: > 0 } cure)
                continue;

            var present = 0;
            foreach (var reagent in cure.Reagents)
            {
                if (ReagentInBody(reagent) >= MinCureReagentAmount)
                    present++;
            }

            // RNA needs any one cure reagent, DNA and superviruses need all at once
            var cured = comp.IsSupervirus || comp.Genome == VirusGenome.Dna
                ? present == cure.Reagents.Count
                : present > 0;

            if (cured)
                SuppressVirus(virus, SuppressDuration);
        }
    }

    private void CollectBodySolutions(EntityUid uid, BloodstreamComponent bloodstream, Solution bloodSolution)
    {
        _bodySolutions.Clear();
        _bodySolutions.Add(bloodSolution);

        if (_solutionContainer.ResolveSolution(uid, bloodstream.MetabolitesSolutionName, ref bloodstream.MetabolitesSolution, out var metabolites))
            _bodySolutions.Add(metabolites);

        if (TryComp<BodyComponent>(uid, out var body) && body.Organs is { } organs)
        {
            foreach (var organ in organs.ContainedEntities)
            {
                if (TryComp<StomachComponent>(organ, out var stomach)
                    && _solutionContainer.ResolveSolution(organ, StomachSystem.DefaultSolutionName, ref stomach.Solution, out var stomachSolution))
                    _bodySolutions.Add(stomachSolution);
            }
        }
    }

    private FixedPoint2 ReagentInBody(ProtoId<ReagentPrototype> reagent)
    {
        var total = FixedPoint2.Zero;
        foreach (var solution in _bodySolutions)
            total += solution.GetTotalPrototypeQuantity(reagent);

        return total;
    }

    private void ApplyVaccines(Entity<VirusHolderComponent> ent)
    {
        _vaccineTotals.Clear();
        foreach (var solution in _bodySolutions)
            AccumulateVaccines(solution, _vaccineTotals);

        foreach (var (reagent, total) in _vaccineTotals)
        {
            if (total < MinVaccineAmount || VirusVaccineData.From(reagent) is not { Strains.Count: > 0 } vaccine)
                continue;

            foreach (var strain in vaccine.Strains)
            {
                if (AddImmunity(ent.Owner, strain))
                {
                    _adminLog.Add(LogType.Virology, LogImpact.Medium,
                        $"{ToPrettyString(ent.Owner):target} was vaccinated against strain {strain}");
                }

                foreach (var virus in GetStrains(ent))
                {
                    if (GetIdentity(virus.Comp) == strain)
                        RemoveVirus(virus);
                }
            }
        }
    }

    private static void AccumulateVaccines(Solution solution, Dictionary<ReagentId, FixedPoint2> totals)
    {
        foreach (var quantity in solution.Contents)
        {
            if (VirusVaccineData.From(quantity.Reagent) is not { Strains.Count: > 0 })
                continue;

            totals.TryGetValue(quantity.Reagent, out var existing);
            totals[quantity.Reagent] = existing + quantity.Quantity;
        }
    }

    private static bool HasVirus(Solution blood)
    {
        foreach (var quantity in blood.Contents)
        {
            if (VirusData.From(quantity.Reagent) is { Viruses.Count: > 0 })
                return true;
        }

        return false;
    }
}
