// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.SS220.Virology;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    private readonly Dictionary<string, VirusMutationPrototype> _mutations = [];
    private readonly Dictionary<string, VirusRevealPrototype> _reveals = [];
    private readonly Dictionary<string, VirusSymptomRemovalPrototype> _removals = [];
    private readonly List<VirusDescriptor> _buffer = [];

    private bool _reacting;

    private void InitializeChemistry()
    {
        BuildTables();
        SubscribeLocalEvent<VirusReactiveComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void BuildTables()
    {
        _mutations.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<VirusMutationPrototype>())
            _mutations[proto.Mutagen] = proto;

        _reveals.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<VirusRevealPrototype>())
            _reveals[proto.Reagent] = proto;

        _removals.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<VirusSymptomRemovalPrototype>())
            _removals[proto.Reagent] = proto;
    }

    private void OnSolutionChanged(Entity<VirusReactiveComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (_reacting)
            return;

        if (_mutations.Count == 0 && _reveals.Count == 0 && _removals.Count == 0)
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, args.SolutionId, out var soln, out _))
            return;

        _reacting = true;
        try
        {
            React(soln.Value);
        }
        finally
        {
            _reacting = false;
        }
    }

    private void React(Entity<SolutionComponent> soln)
    {
        var solution = soln.Comp.Solution;

        _buffer.Clear();
        ReagentId? mutagenId = null;
        VirusMutationPrototype? mutation = null;
        ReagentId? revealId = null;
        VirusRevealPrototype? reveal = null;
        ReagentId? removalId = null;
        VirusSymptomRemovalPrototype? removal = null;

        foreach (var quantity in solution.Contents)
        {
            if (VirusData.From(quantity.Reagent) is { Viruses.Count: > 0 } data)
            {
                _buffer.AddRange(data.Viruses);
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

        if (_buffer.Count == 0)
            return;

        foreach (var descriptor in _buffer)
        {
            if (mutagenId is { } mid && mutation is { Pool.Count: > 0 })
                Mutate(soln, solution, descriptor, mid, mutation);

            if (revealId is { } rid && reveal != null)
                Reveal(soln, solution, descriptor, rid, reveal);

            if (removalId is { } rmid && removal != null)
                Remove(soln, solution, descriptor, rmid, removal);
        }
    }

    private void Mutate(Entity<SolutionComponent> soln, Solution solution, VirusDescriptor virus, ReagentId mutagen, VirusMutationPrototype mutation)
    {
        var mutated = false;
        while (solution.GetTotalPrototypeQuantity(mutagen.Prototype) >= mutation.Cost && TryMutate(virus, mutation))
        {
            _solutionContainer.RemoveReagent(soln, mutagen, mutation.Cost);
            mutated = true;
        }

        if (mutated && mutation.MutateSound != null)
            _audio.PlayPvs(mutation.MutateSound, soln);
    }

    private void Reveal(Entity<SolutionComponent> soln, Solution solution, VirusDescriptor virus, ReagentId reagent, VirusRevealPrototype reveal)
    {
        var revealed = false;
        while (solution.GetTotalPrototypeQuantity(reagent.Prototype) >= reveal.Amount && TryReveal(virus, reveal.Genome))
        {
            _solutionContainer.RemoveReagent(soln, reagent, reveal.Amount);
            revealed = true;
        }

        if (revealed && reveal.RevealSound != null)
            _audio.PlayPvs(reveal.RevealSound, soln);
    }

    private void Remove(Entity<SolutionComponent> soln, Solution solution, VirusDescriptor virus, ReagentId reagent, VirusSymptomRemovalPrototype removal)
    {
        var removed = false;
        while (solution.GetTotalPrototypeQuantity(reagent.Prototype) >= removal.Amount && TryRemoveSymptom(virus))
        {
            _solutionContainer.RemoveReagent(soln, reagent, removal.Amount);
            removed = true;
        }

        if (removed && removal.RemoveSound != null)
            _audio.PlayPvs(removal.RemoveSound, soln);
    }
}
