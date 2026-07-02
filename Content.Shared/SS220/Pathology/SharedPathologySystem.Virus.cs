// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem
{
    private const string DefaultCurePool = "Default";

    /// <summary>Hard cap on how many symptoms a composed virus can hold.</summary>
    public const int MaxVirusSymptoms = 5;

    /// <summary>Symptom cap for a supervirus formed by merging two compatible strains.</summary>
    public const int MaxSupervirusSymptoms = 7;

    /// <summary>How many reagents a supervirus cure has; all of them are needed at once.</summary>
    public const int SupervirusCureCount = 3;

    // Randomize via adding salts so a strain's cure and accelerants 
    private const int CureSalt = 0;
    private const int SupervirusCureSalt = 1;
    private const int AccelerantSalt = 2;
    private const int NameSalt = 3;

    // Lists of reagents to roll from (cure and accelerants)
    private System.Random GetStrainRng(List<ProtoId<PathologyPrototype>> symptoms, int salt)
    {
        return new System.Random(_strainSeed ^ StableHash(GetIdentity(symptoms)) ^ salt);
    }

    // FNV-1a
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = -2128831035; // offset
            foreach (var c in value)
                hash = (hash ^ c) * 16777619; // prime
            return hash;
        }
    }

    private string GenerateMutantName(List<ProtoId<PathologyPrototype>> symptoms)
    {
        var rng = GetStrainRng(symptoms, NameSalt);
        var a = (char)('A' + rng.Next(26));
        var b = (char)('A' + rng.Next(26));
        return Loc.GetString("virus-mutant-name", ("code", $"{a}{b}-{rng.Next(100, 1000)}"));
    }

    private static T Pick<T>(System.Random rng, IReadOnlyList<T> list)
    {
        return list[rng.Next(list.Count)];
    }

    private static T PickAndTake<T>(System.Random rng, List<T> list)
    {
        var index = rng.Next(list.Count);
        var item = list[index];
        list.RemoveAt(index);
        return item;
    }

    public bool TryAddVirus(Entity<PathologyHolderComponent?> entity, ProtoId<VirusPrototype> virusId, out uint instanceId)
    {
        instanceId = 0;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (BuildVirus(virusId) is not { } instance)
            return false;

        if (!AddVirus(entity, instance))
            return false;

        instanceId = instance.Id;
        return true;
    }

    // Builds a standalone virus instance from its prototype.
    public VirusInstance? BuildVirus(ProtoId<VirusPrototype> virusId)
    {
        if (!_prototype.Resolve(virusId, out var virusPrototype))
            return null;

        var instance = new VirusInstance
        {
            Source = virusId,
            Name = virusPrototype.Name is { } name ? Loc.GetString(name) : null,
            Symptoms = new(virusPrototype.Symptoms),
            Transmission = virusPrototype.Transmission,
        };

        RecomputeStrain(instance);
        return instance;
    }

    // Rolls accelerant reagent per symptom from the pool, excluding the strain's own cure reagents.
    private Dictionary<ProtoId<PathologyPrototype>, ProtoId<ReagentPrototype>> RollAccelerants(List<ProtoId<PathologyPrototype>> symptoms, VirusCure? cure)
    {
        var result = new Dictionary<ProtoId<PathologyPrototype>, ProtoId<ReagentPrototype>>();

        if (_net.IsClient || symptoms.Count == 0)
            return result;

        if (!_prototype.TryIndex<DiseaseCurePoolPrototype>(DefaultCurePool, out var pool) || pool.Accelerants.Count == 0)
            return result;

        var candidates = cure is { Reagents.Count: > 0 } c
            ? pool.Accelerants.Where(a => !c.Reagents.Contains(a)).ToList()
            : pool.Accelerants.ToList();

        if (candidates.Count == 0)
            return result;

        var rng = GetStrainRng(symptoms, AccelerantSalt);
        foreach (var symptom in symptoms)
        {
            if (candidates.Count == 0)
                break; // distinct accelerants exhausted — remaining symptoms simply get none (just in case)

            result[symptom] = PickAndTake(rng, candidates);
        }

        return result;
    }

    // Recomputes the symptom-derived fields after the symptom list changes. Order matters:
    // genome first, then cure, then accelerants (which exclude the cure's reagents).
    private void RecomputeStrain(VirusInstance virus)
    {
        virus.CachedIdentity = null;
        virus.Genome = GetGenome(virus.Symptoms);
        virus.Cure = RollCure(virus.Symptoms);
        virus.Accelerants = RollAccelerants(virus.Symptoms, virus.Cure);
    }

    // Virus inherits its genome from its first symptom.
    private VirusGenome GetGenome(List<ProtoId<PathologyPrototype>> symptoms)
    {
        if (symptoms.Count > 0 && _prototype.Resolve(symptoms[0], out var symptom))
            return symptom.Genome;

        return VirusGenome.Rna;
    }

    // Rolls a cure: a random key symptom plus one natural and one synthesized reagent from the pool.
    private VirusCure? RollCure(List<ProtoId<PathologyPrototype>> symptoms)
    {
        if (_net.IsClient || symptoms.Count == 0)
            return null;

        if (!_prototype.TryIndex<DiseaseCurePoolPrototype>(DefaultCurePool, out var pool)
            || pool.Natural.Count == 0
            || pool.Synthesized.Count == 0)
            return null;

        var rng = GetStrainRng(symptoms, CureSalt);
        return new VirusCure
        {
            Reagents = new() { Pick(rng, pool.Natural), Pick(rng, pool.Synthesized) },
        };
    }

    // Splices a symptom into a virus during mutation.
    public bool TryMutateVirus(VirusInstance virus, ProtoId<PathologyPrototype> symptom)
    {
        // a supervirus can't be spliced further
        if (virus.IsSupervirus)
            return false;

        if (virus.Symptoms.Count >= MaxVirusSymptoms)
            return false;

        if (virus.Symptoms.Contains(symptom))
            return false;

        if (!_prototype.Resolve(symptom, out var symptomProto))
            return false;

        if (virus.Symptoms.Count > 0 && symptomProto.Genome != virus.Genome)
            return false;

        virus.Symptoms.Add(symptom);
        RecomputeStrain(virus);

        // a spliced strain is no longer its source prototype — give it its own generated designation
        virus.Source = null;
        virus.Name = GenerateMutantName(virus.Symptoms);
        return true;
    }

    public bool TryRemoveSymptomFromVirus(VirusInstance virus, ProtoId<PathologyPrototype> symptom)
    {
        if (virus.IsSupervirus)
            return false;

        if (virus.Symptoms.Count <= 1)
            return false;

        if (!virus.Symptoms.Remove(symptom))
            return false;

        virus.RevealedSymptoms.Remove(symptom);
        virus.SymptomStages.Remove(symptom);
        RecomputeStrain(virus);
        return true;
    }

    /// <summary>
    /// Adds an already-built virus instance to the entity. Assigns a fresh holder-local id,
    /// so the same instance can be transferred between holders.
    /// </summary>
    public bool AddVirus(Entity<PathologyHolderComponent?> entity, VirusInstance instance)
    {
        // funnel all spread vecrors through here
        if (HasComp<VirusImmunityComponent>(entity.Owner))
            return false;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (IsImmune((entity.Owner, entity.Comp), GetIdentity(instance)))
            return false;

        // second compatible strain fuses with one already present into a supervirus
        if (TryFindMergeable((entity.Owner, entity.Comp), instance, out var existing, out var symptoms))
        {
            MergeInto((entity.Owner, entity.Comp), existing, instance, symptoms);
            return true;
        }

        // otherwise it stands on its own
        AddVirusInstance((entity.Owner, entity.Comp), instance);
        return true;
    }
    public void InfectFromReagent(Entity<PathologyHolderComponent?> entity, ReagentId reagent)
    {
        if (VirusData.From(reagent) is not { Viruses.Count: > 0 } virusData)
            return;

        foreach (var virus in new List<VirusInstance>(virusData.Viruses))
            AddVirus(entity, virus.Clone());
    }

    private void AddVirusInstance(Entity<PathologyHolderComponent> entity, VirusInstance instance)
    {
        instance.Id = entity.Comp.NextVirusId++;
        var context = new VirusPathologyContext { VirusId = instance.Id };

        foreach (var symptom in instance.Symptoms)
        {
            TryAddPathology(entity.Owner, symptom, context);

            if (instance.Accelerants.TryGetValue(symptom, out var accelerant)
                && entity.Comp.ActivePathologies.TryGetValue(symptom, out var data))
                data.Accelerant = accelerant;
        }

        entity.Comp.ActiveViruses[instance.Id] = instance;
        Dirty(entity);
        OnVirusContentsChanged(entity.Owner);
    }

    private bool TryFindMergeable(Entity<PathologyHolderComponent> entity, VirusInstance incoming, [NotNullWhen(true)] out VirusInstance? existing, [NotNullWhen(true)] out List<ProtoId<PathologyPrototype>>? symptoms)
    {
        foreach (var virus in entity.Comp.ActiveViruses.Values)
        {
            if (virus.SuppressedUntil != null || virus.Genome != incoming.Genome)
                continue;

            var union = UnionSymptoms(virus.Symptoms, incoming.Symptoms);
            if (union.Count <= virus.Symptoms.Count || union.Count > MaxSupervirusSymptoms)
                continue;

            if (IsImmune(entity, GetIdentity(union)))
                continue;

            existing = virus;
            symptoms = union;
            return true;
        }

        existing = null;
        symptoms = null;
        return false;
    }

    private void MergeInto(Entity<PathologyHolderComponent> entity, VirusInstance existing, VirusInstance incoming, List<ProtoId<PathologyPrototype>> symptoms)
    {
        var context = new VirusPathologyContext { VirusId = existing.Id };

        foreach (var symptom in symptoms)
        {
            if (!entity.Comp.ActivePathologies.ContainsKey(symptom))
                TryAddPathology(entity.Owner, symptom, context);

            if (entity.Comp.ActivePathologies.TryGetValue(symptom, out var data)
                && _prototype.Resolve(symptom, out var proto))
                AdvancePathologyStage(entity, proto, data);
        }

        existing.Symptoms = symptoms;
        existing.CachedIdentity = null;
        existing.RevealedSymptoms = new();
        existing.Source = null;
        existing.IsSupervirus = true;
        existing.Name = GenerateMutantName(symptoms);
        existing.Transmission = MergeTransmission(existing.Transmission, incoming.Transmission);
        existing.Cure = RollSupervirusCure(symptoms);
        existing.Accelerants = RollAccelerants(symptoms, existing.Cure);

        foreach (var symptom in symptoms)
        {
            if (existing.Accelerants.TryGetValue(symptom, out var accelerant)
                && entity.Comp.ActivePathologies.TryGetValue(symptom, out var data))
                data.Accelerant = accelerant;
        }

        Dirty(entity);
        OnVirusContentsChanged(entity.Owner);
    }

    private VirusCure? RollSupervirusCure(List<ProtoId<PathologyPrototype>> symptoms)
    {
        if (_net.IsClient || symptoms.Count == 0)
            return null;

        if (!_prototype.TryIndex<DiseaseCurePoolPrototype>(DefaultCurePool, out var pool))
            return null;

        var candidates = pool.Natural.Concat(pool.Synthesized).ToList();
        if (candidates.Count < SupervirusCureCount)
            return null;

        var rng = GetStrainRng(symptoms, SupervirusCureSalt);

        var reagents = new List<ProtoId<ReagentPrototype>>();
        for (var i = 0; i < SupervirusCureCount; i++)
            reagents.Add(PickAndTake(rng, candidates));

        return new VirusCure
        {
            Reagents = reagents,
        };
    }

    private static List<ProtoId<PathologyPrototype>> UnionSymptoms(List<ProtoId<PathologyPrototype>> a, List<ProtoId<PathologyPrototype>> b)
    {
        var result = new List<ProtoId<PathologyPrototype>>(a);

        foreach (var symptom in b)
        {
            if (!result.Contains(symptom))
                result.Add(symptom);
        }

        return result;
    }

    private static VirusTransmission? MergeTransmission(VirusTransmission? a, VirusTransmission? b)
    {
        if (a == null)
            return b;

        if (b == null)
            return a;

        return new VirusTransmission
        {
            ContactChance = MathF.Max(a.ContactChance, b.ContactChance),
            ProximityChance = MathF.Max(a.ProximityChance, b.ProximityChance),
            ProximityRange = MathF.Max(a.ProximityRange, b.ProximityRange),
        };
    }

    public bool TryRemoveVirus(Entity<PathologyHolderComponent?> entity, uint instanceId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.ActiveViruses.TryGetValue(instanceId, out var instance))
            return false;

        foreach (var symptom in instance.Symptoms)
            TryChangePathologyStack(entity, symptom, -1);

        entity.Comp.ActiveViruses.Remove(instanceId);
        Dirty(entity);
        OnVirusContentsChanged(entity.Owner);
        return true;
    }

    public bool SuppressVirus(Entity<PathologyHolderComponent?> entity, uint instanceId, TimeSpan duration)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.ActiveViruses.TryGetValue(instanceId, out var instance))
            return false;

        instance.SuppressedStages.Clear();
        foreach (var symptom in instance.Symptoms)
        {
            // remember the stage so reactivation resumes there instead of restarting from stage 1
            if (entity.Comp.ActivePathologies.TryGetValue(symptom, out var data) && data.Level > 0)
                instance.SuppressedStages[symptom] = data.Level;

            TryChangePathologyStack(entity, symptom, -1);
        }

        instance.SuppressedUntil = _gameTiming.CurTime + duration;
        Dirty(entity);
        // re-stamp blood so a drawn sample reflects the now-suppressed strain
        OnVirusContentsChanged(entity.Owner);
        return true;
    }

    // virus becomes active again after supression.
    public bool ReactivateVirus(Entity<PathologyHolderComponent?> entity, uint instanceId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.ActiveViruses.TryGetValue(instanceId, out var instance))
            return false;

        // suppression peeled the symptoms off the host; bringing the strain back re-applies them
        foreach (var symptom in instance.Symptoms)
        {
            // another still-active strain may already drive this symptom — then it's shared,
            // so we only re-claim our stack and leave its stage to the strain that governs it
            var shared = entity.Comp.ActivePathologies.ContainsKey(symptom);

            if (!TryAddPathology(entity, symptom, new VirusPathologyContext { VirusId = instance.Id }))
                continue;

            // climb silently back to the suppressed stage, but only when we're the sole carrier
            if (shared
                || !instance.SuppressedStages.TryGetValue(symptom, out var level)
                || !entity.Comp.ActivePathologies.TryGetValue(symptom, out var data)
                || !_prototype.Resolve(symptom, out var proto))
                continue;

            while (data.Level < level && AdvancePathologyStage((entity.Owner, entity.Comp), proto, data, popup: false))
            {
            }
        }

        instance.SuppressedStages.Clear();
        instance.SuppressedUntil = null;
        Dirty(entity);
        // re-stamp blood so a drawn sample reflects the now-active strain
        OnVirusContentsChanged(entity.Owner);
        return true;
    }

    public string GetIdentity(VirusInstance virus)
    {
        // cache is reset wherever Symptoms changes (RecomputeStrain, MergeInto) and assert catches a missed spot
        DebugTools.Assert(virus.CachedIdentity == null || virus.CachedIdentity == GetIdentity(virus.Symptoms),
            "VirusInstance.CachedIdentity is stale — Symptoms changed without resetting it");

        return virus.CachedIdentity ??= GetIdentity(virus.Symptoms);
    }

    public string GetIdentity(List<ProtoId<PathologyPrototype>> symptoms)
    {
        return string.Join(",", symptoms.Select(s => s.Id).OrderBy(id => id, StringComparer.Ordinal));
    }

    public bool IsImmune(Entity<PathologyHolderComponent> entity, string identity)
    {
        if (entity.Comp.Immunities.Contains(identity))
            return true;

        foreach (var virus in entity.Comp.ActiveViruses.Values)
        {
            if (GetIdentity(virus) == identity)
                return true;
        }

        return false;
    }

    public void AddImmunity(Entity<PathologyHolderComponent?> entity, string identity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (entity.Comp.Immunities.Add(identity))
            Dirty(entity);
    }

    protected virtual void OnVirusContentsChanged(EntityUid uid) { }

    public bool TryGetVirus(Entity<PathologyHolderComponent?> entity, uint instanceId, [NotNullWhen(true)] out VirusInstance? instance)
    {
        instance = null;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        return entity.Comp.ActiveViruses.TryGetValue(instanceId, out instance);
    }
}
