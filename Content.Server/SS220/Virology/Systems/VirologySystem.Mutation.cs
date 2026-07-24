// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Database;
using Content.Shared.SS220.Virology;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem
{
    public bool TryMutate(Entity<VirusComponent> virus, VirusMutationPrototype mutation)
    {
        if (virus.Comp.IsSupervirus || virus.Comp.SymptomStates.Count >= MaxSymptoms)
            return false;

        if (!TryPickSymptom(mutation.Pool, virus.Comp.Genome, virus.Comp.SymptomStates.Keys, out var picked))
            return false;

        virus.Comp.SymptomStates[picked.Value] = new VirusSymptomState
        {
            StageStartTime = _timing.CurTime,
            LastEmote = _timing.CurTime,
            Accelerant = RollAccelerant(CollectAccelerants(virus.Comp)),
        };
        ApplyStage(virus, picked.Value, -1, 0);
        virus.Comp.Source = null;
        virus.Comp.Name = GenerateName();
        virus.Comp.CachedIdentity = null; // symptom set changed — force identity recompute
        Dirty(virus);
        RaiseContentsChanged(virus.Comp.Carrier);
        return true;
    }

    public bool TryMutate(VirusDescriptor descriptor, VirusMutationPrototype mutation)
    {
        if (descriptor.IsSupervirus || descriptor.Symptoms.Count >= MaxSymptoms)
            return false;

        if (!TryPickSymptom(mutation.Pool, descriptor.Genome, descriptor.Symptoms.Select(s => s.Symptom), out var picked))
            return false;

        var used = CollectAccelerants(descriptor);
        descriptor.Symptoms.Add(new VirusSymptomSnapshot { Symptom = picked.Value, Accelerant = RollAccelerant(used) });
        descriptor.Cure = ResolveCure(descriptor);
        descriptor.Transmission = ResolveTransmission(descriptor);
        descriptor.Source = null;
        descriptor.Name = GenerateName();
        return true;
    }

    public bool TryReveal(Entity<VirusComponent> virus, VirusGenome genome)
    {
        if (virus.Comp.Genome != genome)
            return false;

        var hidden = virus.Comp.SymptomStates.Where(kv => !kv.Value.Revealed).Select(kv => kv.Key).ToList();
        if (hidden.Count == 0)
            return false;

        virus.Comp.SymptomStates[_random.Pick(hidden)].Revealed = true;
        Dirty(virus);
        RaiseContentsChanged(virus.Comp.Carrier);
        return true;
    }

    public bool TryReveal(VirusDescriptor descriptor, VirusGenome genome)
    {
        if (descriptor.Genome != genome)
            return false;

        var hidden = descriptor.Symptoms.Where(s => !s.Revealed).ToList();
        if (hidden.Count == 0)
            return false;

        _random.Pick(hidden).Revealed = true;
        return true;
    }

    public bool TryRemoveSymptom(Entity<VirusComponent> virus)
    {
        if (virus.Comp.IsSupervirus || virus.Comp.SymptomStates.Count <= 1)
            return false;

        var pick = _random.Pick(virus.Comp.SymptomStates.Keys.ToList());
        ApplyStage(virus, pick, virus.Comp.SymptomStates[pick].Stage, -1);
        virus.Comp.SymptomStates.Remove(pick);
        virus.Comp.CachedIdentity = null; // symptom set changed — force identity recompute
        Dirty(virus);
        RaiseContentsChanged(virus.Comp.Carrier);
        return true;
    }

    public bool TryRemoveSymptom(VirusDescriptor descriptor)
    {
        if (descriptor.IsSupervirus || descriptor.Symptoms.Count <= 1)
            return false;

        descriptor.Symptoms.RemoveAt(_random.Next(descriptor.Symptoms.Count));
        return true;
    }

    /// <summary>Merges strains forming a supervirus.</summary>
    public bool MergeDescriptor(Entity<VirusComponent> target, VirusDescriptor incoming)
    {
        if (target.Comp.IsSupervirus || target.Comp.Genome != incoming.Genome)
            return false;

        var added = false;
        var used = CollectAccelerants(target.Comp);
        foreach (var snapshot in incoming.Symptoms)
        {
            if (target.Comp.SymptomStates.Count >= MaxSupervirusSymptoms)
                break;

            if (target.Comp.SymptomStates.ContainsKey(snapshot.Symptom))
                continue;

            // keep accelerant unless its a double
            var accelerant = snapshot.Accelerant is { } incoming2 && !used.Contains(incoming2)
                ? snapshot.Accelerant
                : RollAccelerant(used);
            if (accelerant is { } addedAccelerant)
                used.Add(addedAccelerant);

            target.Comp.SymptomStates[snapshot.Symptom] = new VirusSymptomState
            {
                Stage = snapshot.Stage,
                StageStartTime = _timing.CurTime,
                LastEmote = _timing.CurTime,
                Accelerant = accelerant,
            };
            ApplyStage(target, snapshot.Symptom, -1, snapshot.Stage);
            added = true;
        }

        if (!added)
            return false;

        target.Comp.IsSupervirus = true;
        target.Comp.Source = null;
        target.Comp.Name = GenerateName();
        target.Comp.Cure = RollCure(SupervirusCureCount);
        target.Comp.Transmission = MergeTransmission(target.Comp.Transmission, incoming.Transmission);
        target.Comp.CachedIdentity = null;
        Dirty(target);
        RaiseContentsChanged(target.Comp.Carrier);

        _adminLog.Add(LogType.Virology, LogImpact.Medium,
            $"{ToPrettyString(target.Comp.Carrier):target} strains merged into supervirus {DescribeVirus(target.Comp)}");
        return true;
    }

    private bool TryPickSymptom(List<ProtoId<VirusSymptomPrototype>> pool, VirusGenome genome, IEnumerable<ProtoId<VirusSymptomPrototype>> present, [NotNullWhen(true)] out ProtoId<VirusSymptomPrototype>? picked)
    {
        picked = null;
        var presentSet = new HashSet<ProtoId<VirusSymptomPrototype>>(present);
        var candidates = new List<ProtoId<VirusSymptomPrototype>>();
        foreach (var symptom in pool)
        {
            if (presentSet.Contains(symptom))
                continue;

            if (!_proto.Resolve(symptom, out var proto) || proto.Genome != genome)
                continue;

            candidates.Add(symptom);
        }

        if (candidates.Count == 0)
            return false;

        picked = _random.Pick(candidates);
        return true;
    }

    private string GenerateName()
    {
        var a = (char)('A' + _random.Next(26));
        var b = (char)('A' + _random.Next(26));
        return Loc.GetString("virus-mutant-name", ("code", $"{a}{b}-{_random.Next(100, 1000)}"));
    }

    private static VirusTransmission? MergeTransmission(VirusTransmission? a, VirusTransmission? b)
    {
        if (a == null)
            return b?.Clone();

        if (b == null)
            return a.Clone();

        return new VirusTransmission
        {
            ContactChance = MathF.Max(a.ContactChance, b.ContactChance),
            ProximityChance = MathF.Max(a.ProximityChance, b.ProximityChance),
            ProximityRange = MathF.Max(a.ProximityRange, b.ProximityRange),
        };
    }
}
