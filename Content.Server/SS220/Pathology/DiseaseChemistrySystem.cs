// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Pathology;

public sealed partial class DiseaseChemistrySystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly FixedPoint2 MutateCost = FixedPoint2.New(1);

    private readonly Dictionary<string, VirusMutationPrototype> _mutations = new();
    private readonly Dictionary<string, DiseaseRevealPrototype> _reveals = new();
    private readonly Dictionary<string, DiseaseSymptomRemovalPrototype> _removals = new();
    private readonly List<VirusInstance> _virusBuffer = new();

    private bool _reacting;

    public override void Initialize()
    {
        base.Initialize();

        BuildTables();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<SolutionComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<VirusMutationPrototype>() || args.WasModified<DiseaseRevealPrototype>()
            || args.WasModified<DiseaseSymptomRemovalPrototype>())
            BuildTables();
    }

    private void BuildTables()
    {
        _mutations.Clear();
        foreach (var proto in _prototype.EnumeratePrototypes<VirusMutationPrototype>())
            _mutations[proto.Mutagen] = proto;

        _reveals.Clear();
        foreach (var proto in _prototype.EnumeratePrototypes<DiseaseRevealPrototype>())
            _reveals[proto.Reagent] = proto;

        _removals.Clear();
        foreach (var proto in _prototype.EnumeratePrototypes<DiseaseSymptomRemovalPrototype>())
            _removals[proto.Reagent] = proto;
    }

    private void OnSolutionChanged(Entity<SolutionComponent> ent, ref SolutionChangedEvent args)
    {
        if (_reacting)
            return;

        if (_mutations.Count == 0 && _reveals.Count == 0 && _removals.Count == 0)
            return;

        _reacting = true;
        try
        {
            React(ent);
        }
        finally
        {
            _reacting = false;
        }
    }

    private void React(Entity<SolutionComponent> soln)
    {
        var solution = soln.Comp.Solution;

        // one check over contents for everything
        _virusBuffer.Clear();
        ReagentId? mutagenId = null;
        VirusMutationPrototype? mutation = null;
        ReagentId? revealId = null;
        DiseaseRevealPrototype? reveal = null;
        ReagentId? removalId = null;
        DiseaseSymptomRemovalPrototype? removal = null;

        foreach (var quantity in solution.Contents)
        {
            if (VirusData.From(quantity.Reagent) is { Viruses.Count: > 0 } virusData)
            {
                _virusBuffer.AddRange(virusData.Viruses);
                continue;
            }

            if (mutation == null && _mutations.TryGetValue(quantity.Reagent.Prototype, out var m))
            {
                mutagenId = quantity.Reagent;
                mutation = m;
            }

            if (reveal == null && _reveals.TryGetValue(quantity.Reagent.Prototype, out var r))
            {
                revealId = quantity.Reagent;
                reveal = r;
            }

            if (removal == null && _removals.TryGetValue(quantity.Reagent.Prototype, out var rm))
            {
                removalId = quantity.Reagent;
                removal = rm;
            }
        }

        if (_virusBuffer.Count == 0)
            return;

        // every strain in the sample reacts, not just the first; each reaction drains the reagent
        // as it works, so later strains get whatever dose the earlier ones left
        foreach (var virus in _virusBuffer)
        {
            if (mutagenId is { } mid && mutation is { Pool.Count: > 0 })
                Mutate(soln, solution, virus, mid, mutation);

            if (revealId is { } rid && reveal != null)
                Reveal(soln, solution, virus, rid, reveal);

            if (removalId is { } rmid && removal != null)
                Remove(soln, solution, virus, rmid, removal);
        }
    }

    private void Mutate(Entity<SolutionComponent> soln, Solution solution, VirusInstance virus, ReagentId mutagen, VirusMutationPrototype mutation)
    {
        // a supervirus is final — don't drain mutagen or play the sound for nothing
        if (virus.IsSupervirus)
            return;

        var mutated = false;
        while (solution.GetTotalPrototypeQuantity(mutagen.Prototype) >= MutateCost
               && virus.Symptoms.Count < SharedPathologySystem.MaxVirusSymptoms)
        {
            _pathology.TryMutateVirus(virus, _random.Pick(mutation.Pool));

            _solutionContainer.RemoveReagent(soln, mutagen, MutateCost);
            mutated = true;
        }

        if (mutated)
            _audio.PlayPvs(mutation.MutateSound, soln);
    }

    private void Reveal(Entity<SolutionComponent> soln, Solution solution, VirusInstance virus, ReagentId reagent, DiseaseRevealPrototype reveal)
    {
        // r only reveals symptoms of its own genome
        if (virus.Genome != reveal.Genome)
            return;

        var revealed = false;
        while (solution.GetTotalPrototypeQuantity(reagent.Prototype) >= reveal.Amount
               && virus.RevealedSymptoms.Count < virus.Symptoms.Count)
        {
            // random symptom, so r exposes a hidden one, or wastes dose on an already-known one
            if (virus.RevealedSymptoms.Add(_random.Pick(virus.Symptoms)))
                revealed = true;

            _solutionContainer.RemoveReagent(soln, reagent, reveal.Amount);
        }

        if (revealed)
            _audio.PlayPvs(reveal.RevealSound, soln);
    }

    private void Remove(Entity<SolutionComponent> soln, Solution solution, VirusInstance virus, ReagentId reagent, DiseaseSymptomRemovalPrototype removal)
    {
        // a supervirus is final — don't drain the reagent or play the sound for nothing
        if (virus.IsSupervirus)
            return;

        var removed = false;
        // never strips last symptom so virus keeps at least one (probably there is better logic. somewhere)
        while (solution.GetTotalPrototypeQuantity(reagent.Prototype) >= removal.Amount
               && virus.Symptoms.Count > 1)
        {
            if (!_pathology.TryRemoveSymptomFromVirus(virus, _random.Pick(virus.Symptoms)))
                break;

            _solutionContainer.RemoveReagent(soln, reagent, removal.Amount);
            removed = true;
        }

        if (removed)
            _audio.PlayPvs(removal.RemoveSound, soln);
    }
}
