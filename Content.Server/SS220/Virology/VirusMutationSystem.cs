// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.SS220.Virology;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology;

public sealed partial class VirusMutationSystem : EntitySystem
{
    [Dependency] private VirologySystem _virology = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;

    public bool TryMutate(Entity<VirusComponent> virus, VirusMutationPrototype mutation)
    {
        if (virus.Comp.IsSupervirus || virus.Comp.Symptoms.Count >= VirologySystem.MaxSymptoms)
            return false;

        if (!TryPickSymptom(mutation.Pool, virus.Comp.Genome, virus.Comp.Symptoms.Keys, out var picked))
            return false;

        virus.Comp.Symptoms[picked.Value] = new VirusSymptomState
        {
            StageStartTime = _timing.CurTime,
            LastEmote = _timing.CurTime,
            Accelerant = _virology.RollAccelerant(VirologySystem.CollectAccelerants(virus.Comp)),
        };
        _virology.ApplyStage(virus, picked.Value, -1, 0);
        virus.Comp.Source = null;
        virus.Comp.Name = GenerateName();
        virus.Comp.CachedIdentity = null; // symptom set changed — force identity recompute
        Dirty(virus);
        _virology.RaiseContentsChanged(virus.Comp.Carrier);
        return true;
    }

    public bool TryMutate(VirusDescriptor descriptor, VirusMutationPrototype mutation)
    {
        if (descriptor.IsSupervirus || descriptor.Symptoms.Count >= VirologySystem.MaxSymptoms)
            return false;

        if (!TryPickSymptom(mutation.Pool, descriptor.Genome, descriptor.Symptoms.Select(s => s.Symptom), out var picked))
            return false;

        var used = VirologySystem.CollectAccelerants(descriptor);
        descriptor.Symptoms.Add(new VirusSymptomSnapshot { Symptom = picked.Value, Accelerant = _virology.RollAccelerant(used) });
        descriptor.Source = null;
        descriptor.Name = GenerateName();
        return true;
    }

    public bool TryReveal(Entity<VirusComponent> virus, VirusGenome genome)
    {
        if (virus.Comp.Genome != genome)
            return false;

        var hidden = virus.Comp.Symptoms.Where(kv => !kv.Value.Revealed).Select(kv => kv.Key).ToList();
        if (hidden.Count == 0)
            return false;

        virus.Comp.Symptoms[_random.Pick(hidden)].Revealed = true;
        Dirty(virus);
        _virology.RaiseContentsChanged(virus.Comp.Carrier);
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
        if (virus.Comp.IsSupervirus || virus.Comp.Symptoms.Count <= 1)
            return false;

        var pick = _random.Pick(virus.Comp.Symptoms.Keys.ToList());
        _virology.ApplyStage(virus, pick, virus.Comp.Symptoms[pick].Stage, -1);
        virus.Comp.Symptoms.Remove(pick);
        virus.Comp.CachedIdentity = null; // symptom set changed — force identity recompute
        Dirty(virus);
        _virology.RaiseContentsChanged(virus.Comp.Carrier);
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
        var used = VirologySystem.CollectAccelerants(target.Comp);
        foreach (var snapshot in incoming.Symptoms)
        {
            if (target.Comp.Symptoms.Count >= VirologySystem.MaxSupervirusSymptoms)
                break;

            if (target.Comp.Symptoms.ContainsKey(snapshot.Symptom))
                continue;

            // keep accelerant unless its a double
            var accelerant = snapshot.Accelerant is { } incoming2 && !used.Contains(incoming2)
                ? snapshot.Accelerant
                : _virology.RollAccelerant(used);
            if (accelerant is { } addedAccelerant)
                used.Add(addedAccelerant);

            target.Comp.Symptoms[snapshot.Symptom] = new VirusSymptomState
            {
                Stage = snapshot.Stage,
                StageStartTime = _timing.CurTime,
                LastEmote = _timing.CurTime,
                Accelerant = accelerant,
            };
            _virology.ApplyStage(target, snapshot.Symptom, -1, snapshot.Stage);
            added = true;
        }

        if (!added)
            return false;

        target.Comp.IsSupervirus = true;
        target.Comp.Source = null;
        target.Comp.Name = GenerateName();
        target.Comp.Cure = _virology.RollCure(VirologySystem.SupervirusCureCount);
        target.Comp.Transmission = MergeTransmission(target.Comp.Transmission, incoming.Transmission);
        target.Comp.CachedIdentity = null;
        Dirty(target);
        _virology.RaiseContentsChanged(target.Comp.Carrier);

        _adminLog.Add(LogType.Virology, LogImpact.Medium,
            $"{ToPrettyString(target.Comp.Carrier):target} strains merged into supervirus {VirologySystem.DescribeVirus(target.Comp)}");
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
