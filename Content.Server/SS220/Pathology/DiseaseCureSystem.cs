// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Pathology;

public sealed partial class DiseaseCureSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SuppressDuration = TimeSpan.FromMinutes(15);
    private static readonly FixedPoint2 MinAmount = 5;
    private static readonly FixedPoint2 VaccineAmount = 5;

    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + Interval;

        var query = EntityQueryEnumerator<PathologyHolderComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var holder, out var blood))
        {
            if (!_solutionContainer.ResolveSolution(uid, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodSolution)
                || blood.BloodSolution is not { } bloodSolnEntity)
                continue;

            // AddVirus rejects the host's own strains and anything it's immune to, so only foreign
            // strains taken. Works on a corpse too, it just becomes a dormant carrier until revived.
            InfectFromBlood((uid, holder), bloodSolution);

            // corpse doesn't metabolise, so reagent cures and vaccines in its blood have no effect
            if (_mobState.IsDead(uid))
                continue;

            UpdateViruses((uid, holder), bloodSolution);
            ApplyVaccines((uid, holder), bloodSolnEntity, bloodSolution);
        }
    }

    private void InfectFromBlood(Entity<PathologyHolderComponent> ent, Solution blood)
    {
        // successful infection re-stamps and consolidates the blood (mutating Contents), so the
        // defensive copy below is needed — but only when blood actually carries a virus, which is rare
        if (!HasVirus(blood))
            return;

        foreach (var quantity in new List<ReagentQuantity>(blood.Contents))
            _pathology.InfectFromReagent(ent!, quantity.Reagent);
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

    private static bool HasVaccine(Solution blood)
    {
        foreach (var quantity in blood.Contents)
        {
            if (quantity.Quantity >= VaccineAmount && VaccineData.From(quantity.Reagent) is { Strains.Count: > 0 })
                return true;
        }

        return false;
    }

    private void UpdateViruses(Entity<PathologyHolderComponent> ent, Solution blood)
    {
        if (ent.Comp.ActiveViruses.Count == 0)
            return;

        foreach (var virus in new List<VirusInstance>(ent.Comp.ActiveViruses.Values))
        {
            if (virus.SuppressedUntil is { } until)
            {
                if (_timing.CurTime >= until)
                    _pathology.ReactivateVirus(ent!, virus.Id);

                continue;
            }

            if (virus.Cure is not { Reagents.Count: > 0 } cure)
                continue;

            var present = 0;
            foreach (var reagent in cure.Reagents)
            {
                if (blood.GetTotalPrototypeQuantity(reagent) >= MinAmount)
                    present++;
            }

            var cured = virus.IsSupervirus || virus.Genome == VirusGenome.Dna
                ? present == cure.Reagents.Count
                : present > 0;

            if (cured)
                _pathology.SuppressVirus(ent!, virus.Id, SuppressDuration);
        }
    }

    private void ApplyVaccines(Entity<PathologyHolderComponent> ent, Entity<SolutionComponent> bloodSoln, Solution blood)
    {
        // RemoveReagent below mutates Contents, so iterate a copy — but only bother when a vaccine is
        // actually present (the common case is blood with none)
        if (!HasVaccine(blood))
            return;

        foreach (var quantity in new List<ReagentQuantity>(blood.Contents))
        {
            if (quantity.Quantity < VaccineAmount || VaccineData.From(quantity.Reagent) is not { Strains.Count: > 0 } vaccine)
                continue;

            foreach (var strain in vaccine.Strains)
            {
                _pathology.AddImmunity(ent!, strain);

                foreach (var existing in new List<VirusInstance>(ent.Comp.ActiveViruses.Values))
                {
                    if (_pathology.GetIdentity(existing) == strain)
                        _pathology.TryRemoveVirus(ent!, existing.Id);
                }
            }

            _solutionContainer.RemoveReagent(bloodSoln, quantity.Reagent.Prototype, VaccineAmount);
        }
    }
}
