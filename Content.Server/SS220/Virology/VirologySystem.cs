// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Rejuvenate;
using Content.Shared.SS220.Virology;
using Content.Shared.SS220.Virology.Effects;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology;

public sealed partial class VirologySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private VirusMutationSystem _mutation = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private VirusOverloadRuleSystem _overload = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;

    /// <summary>Generic entity every virus strain spawns from, its symptoms/state come from descriptor.</summary>
    public const string BaseVirusProto = "BaseVirus";

    /// <summary>Max symptoms a normal strain can hold.</summary>
    public const int MaxSymptoms = 5;

    /// <summary>Max symptoms a supervirus can hold.</summary>
    public const int MaxSupervirusSymptoms = 7;

    /// <summary>How many reagents a supervirus cure needs (all present at once).</summary>
    public const int SupervirusCureCount = 3;

    /// <summary>How many reagents a normal (non-super) virus cure is rolled with.</summary>
    public const int NormalCureCount = 2;

    /// <summary>Cure pool the per-symptom accelerants are rolled from.</summary>
    private const string AccelerantPool = "Default";

    // reused when building a strain identity
    private readonly List<string> _identityBuf = [];

    // reused when picking a distinct accelerant
    private readonly List<ProtoId<ReagentPrototype>> _accelerantBuf = [];

    // reused when rolling a cure from pool
    private readonly List<ProtoId<ReagentPrototype>> _cureBuf = [];

    // cure rolled once per prototype per round
    private readonly Dictionary<ProtoId<VirusPrototype>, VirusCure?> _roundCures = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusHolderComponent, ComponentShutdown>(OnHolderShutdown);
        SubscribeLocalEvent<VirusComponent, ComponentShutdown>(OnVirusShutdown);
        SubscribeLocalEvent<VirusComponent, VirusDoseAbsorbedEvent>(OnDoseAbsorbed);
        SubscribeLocalEvent<VirusSusceptibleComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<VirusHolderComponent, EntityZombifiedEvent>(OnZombified);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    // becoming zombie kills the host's viruses
    private void OnZombified(Entity<VirusHolderComponent> ent, ref EntityZombifiedEvent args)
    {
        foreach (var virus in GetStrains(ent))
            RemoveVirus(virus);
    }

    private void OnRejuvenate(Entity<VirusSusceptibleComponent> ent, ref RejuvenateEvent args)
    {
        foreach (var virus in GetStrains(ent))
            RemoveVirus(virus);

        RemComp<VirusImmunitiesComponent>(ent);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _roundCures.Clear();
    }

    public VirusCure? GetRoundCure(ProtoId<VirusPrototype> protoId)
    {
        if (_roundCures.TryGetValue(protoId, out var cached))
            return cached;

        var cure = RollCure(NormalCureCount);
        _roundCures[protoId] = cure;
        return cure;
    }

    public VirusCure? RollCure(int count)
    {
        if (!_proto.Resolve<VirusCurePoolPrototype>(AccelerantPool, out var pool))
            return null;

        _cureBuf.Clear();
        _cureBuf.AddRange(pool.Natural);
        _cureBuf.AddRange(pool.Synthesized);
        if (_cureBuf.Count == 0)
            return null;

        var cure = new VirusCure();
        for (var i = 0; i < count && _cureBuf.Count > 0; i++)
        {
            var index = _random.Next(_cureBuf.Count);
            cure.Reagents.Add(_cureBuf[index]);
            _cureBuf.RemoveAt(index);
        }

        return cure;
    }

    // absorbing an accelerant dose re-randomises which reagent drives that symptom so no insta 1->max stages
    private void OnDoseAbsorbed(Entity<VirusComponent> ent, ref VirusDoseAbsorbedEvent args)
    {
        args.Symptom.Accelerant = RollAccelerant(CollectAccelerants(ent.Comp, args.Symptom));
        Dirty(ent);
    }

    private void OnVirusShutdown(Entity<VirusComponent> ent, ref ComponentShutdown args)
    {
        var carrier = ent.Comp.Carrier;
        if (Exists(carrier) && !Terminating(carrier))
        {
            if (TryComp<VirusHolderComponent>(carrier, out var holder))
                holder.Viruses.Remove(ent.Owner);

            var othersHold = GetComponentsGrantedByOthers(carrier, ent.Owner, null);
            foreach (var granted in ent.Comp.GrantedComponents.Values)
            {
                foreach (var name in granted)
                {
                    // keep a component another live strain/symptom on this carrier still requires
                    if (!othersHold.Contains(name) && _factory.TryGetRegistration(name, out var reg))
                        RemComp(carrier, reg.Type);
                }
            }
        }

        ent.Comp.GrantedComponents.Clear();
    }

    private void OnHolderShutdown(Entity<VirusHolderComponent> ent, ref ComponentShutdown args)
    {
        // viruses live in nullspace, so they don't die with it, we delete them ourselves
        foreach (var virus in ent.Comp.Viruses)
            QueueDel(virus);

        if (Terminating(ent.Owner))
            return;

        _overload.Deactivate(ent.Owner);
    }

    #region Infection gate

    /// Infects a host a strain built from its prototype
    public bool AddVirus(EntityUid host, ProtoId<VirusPrototype> protoId)
    {
        return BuildDescriptor(protoId) is { } descriptor && AddVirus(host, descriptor);
    }

    public bool AddVirus(EntityUid host, VirusDescriptor descriptor)
    {
        if (!HasComp<VirusSusceptibleComponent>(host) || HasComp<VirusImmunityComponent>(host))
            return false;

        var identity = GetIdentity(descriptor);

        if (TryComp<VirusImmunitiesComponent>(host, out var immunities) && immunities.Strains.Contains(identity))
            return false;

        Entity<VirusComponent>? mergeTarget = null;
        foreach (var carried in EnumerateStrains(host))
        {
            if (GetIdentity(carried.Comp) == identity)
                return false;

            // a different but compatible strain fuses into a supervirus
            if (!carried.Comp.IsSupervirus && carried.Comp.Genome == descriptor.Genome)
                mergeTarget = carried;
        }

        if (mergeTarget is { } target)
        {
            var targetVirus = target.Comp;

            // if already immune to supervirus, don't let merge into it (idk best i came up with)
            if (immunities != null && immunities.Strains.Contains(GetUnionIdentity(targetVirus, descriptor)))
                return SpawnVirus(host, descriptor) != null;

            if (targetVirus.SuppressedUntil != null)
                ReactivateVirus(target);

            return _mutation.MergeDescriptor(target, descriptor);
        }

        return SpawnVirus(host, descriptor) != null;
    }

    public void InfectFromReagent(EntityUid host, ReagentId reagent, bool bloodborne = false)
    {
        if (VirusData.From(reagent) is not { Viruses.Count: > 0 } data)
            return;

        foreach (var descriptor in new List<VirusDescriptor>(data.Viruses))
        {
            if (!bloodborne && IsBloodOnly(descriptor))
                continue;

            AddVirus(host, descriptor.Clone());
        }
    }

    /// <summary>True if any of the strain's symptoms restrict it to blood-borne spread only.</summary>
    public bool IsBloodOnly(VirusComponent virus)
    {
        foreach (var symptomId in virus.Symptoms.Keys)
        {
            if (_proto.Resolve(symptomId, out var symptom) && symptom.BloodBorneOnly)
                return true;
        }

        return false;
    }

    public bool IsBloodOnly(VirusDescriptor descriptor)
    {
        foreach (var snapshot in descriptor.Symptoms)
        {
            if (_proto.Resolve(snapshot.Symptom, out var symptom) && symptom.BloodBorneOnly)
                return true;
        }

        return false;
    }

    #endregion

    #region Descriptor building

    public VirusDescriptor? BuildDescriptor(ProtoId<VirusPrototype> protoId)
    {
        if (!_proto.Resolve(protoId, out var proto))
            return null;

        var descriptor = new VirusDescriptor
        {
            Source = protoId,
            Name = proto.Name is { } name ? Loc.GetString(name) : null,
            Genome = GetGenome(proto.Symptoms),
            Cure = GetRoundCure(protoId)?.Clone(),
            Transmission = proto.Transmission?.Clone(),
        };

        var used = new HashSet<ProtoId<ReagentPrototype>>();
        foreach (var symptom in proto.Symptoms)
        {
            var accelerant = RollAccelerant(used);
            if (accelerant is { } picked)
                used.Add(picked);

            descriptor.Symptoms.Add(new VirusSymptomSnapshot { Symptom = symptom, Accelerant = accelerant });
        }

        return descriptor;
    }

    public VirusDescriptor ToDescriptor(Entity<VirusComponent> virus)
    {
        var descriptor = new VirusDescriptor
        {
            Source = virus.Comp.Source,
            Name = virus.Comp.Name,
            Genome = virus.Comp.Genome,
            IsSupervirus = virus.Comp.IsSupervirus,
            Cure = virus.Comp.Cure?.Clone(),
            Transmission = virus.Comp.Transmission?.Clone(),
            SuppressedRemaining = virus.Comp.SuppressedUntil is { } until ? until - _timing.CurTime : null,
        };

        foreach (var (symptomId, state) in virus.Comp.Symptoms)
        {
            descriptor.Symptoms.Add(new VirusSymptomSnapshot
            {
                Symptom = symptomId,
                Stage = state.Stage,
                Revealed = state.Revealed,
                Accelerant = state.Accelerant,
            });
        }

        return descriptor;
    }

    public EntityUid? SpawnVirus(EntityUid host, VirusDescriptor descriptor)
    {
        var holder = EnsureHolder(host);

        var virus = Spawn(BaseVirusProto);
        var comp = EnsureComp<VirusComponent>(virus);
        comp.Carrier = host;
        comp.Source = descriptor.Source;
        comp.Name = descriptor.Name;
        comp.Genome = descriptor.Genome;
        comp.Transmission = descriptor.Transmission?.Clone();
        comp.Cure = descriptor.Cure?.Clone();
        comp.IsSupervirus = descriptor.IsSupervirus;

        foreach (var snapshot in descriptor.Symptoms)
        {
            comp.Symptoms[snapshot.Symptom] = new VirusSymptomState
            {
                Stage = snapshot.Stage,
                StageStartTime = _timing.CurTime,
                LastEmote = _timing.CurTime,
                Revealed = snapshot.Revealed,
                Accelerant = snapshot.Accelerant,
            };
        }

        // if infected with suppressed strain - spawns suppressed and keeps timer
        if (descriptor.SuppressedRemaining is { } remaining && remaining > TimeSpan.Zero)
            comp.SuppressedUntil = _timing.CurTime + remaining;

        Dirty(virus, comp);

        // virus kept in nullspace (server-only, never networked)
        holder.Viruses.Add(virus);

        if (comp.SuppressedUntil == null)
        {
            foreach (var (symptomId, state) in comp.Symptoms)
                ApplyStage((virus, comp), symptomId, -1, state.Stage);
        }

        RaiseContentsChanged(host);

        _adminLog.Add(LogType.Virology, LogImpact.Medium,
            $"{ToPrettyString(host):target} was infected with virus {DescribeVirus(comp)}");

        return virus;
    }

    // for admin logs
    public static string DescribeVirus(VirusComponent comp)
    {
        var name = comp.Name ?? comp.Source?.Id ?? "mutant";
        var symptoms = string.Join(", ", comp.Symptoms.Keys);
        return $"'{name}' [{comp.Genome}{(comp.IsSupervirus ? ", supervirus" : "")}] symptoms: {symptoms}";
    }

    public void RemoveVirus(Entity<VirusComponent> virus)
    {
        var carrier = virus.Comp.Carrier;
        Del(virus.Owner);

        RaiseContentsChanged(carrier);

        if (TryComp<VirusHolderComponent>(carrier, out var holder) && holder.Viruses.Count == 0)
            RemComp<VirusHolderComponent>(carrier);
    }

    public void RaiseContentsChanged(EntityUid carrier)
    {
        var ev = new VirusContentsChangedEvent();
        RaiseLocalEvent(carrier, ref ev);
    }

    private static readonly HashSet<EntityUid> EmptyStrains = [];

    public StrainQuery EnumerateStrains(EntityUid host)
        => new(EntityManager, TryComp<VirusHolderComponent>(host, out var holder) ? holder.Viruses : EmptyStrains);

    public StrainQuery EnumerateStrains(VirusHolderComponent holder) => new(EntityManager, holder.Viruses);

    public List<Entity<VirusComponent>> GetStrains(EntityUid host)
    {
        var list = new List<Entity<VirusComponent>>();
        foreach (var strain in EnumerateStrains(host))
            list.Add(strain);

        return list;
    }

    public readonly struct StrainQuery(IEntityManager entMan, HashSet<EntityUid> viruses)
    {
        public Enumerator GetEnumerator() => new(entMan, viruses);

        public struct Enumerator(IEntityManager entMan, HashSet<EntityUid> viruses)
        {
            private HashSet<EntityUid>.Enumerator _inner = viruses.GetEnumerator();

            public Entity<VirusComponent> Current { get; private set; }

            public bool MoveNext()
            {
                while (_inner.MoveNext())
                {
                    if (entMan.TryGetComponent<VirusComponent>(_inner.Current, out var comp))
                    {
                        Current = (_inner.Current, comp);
                        return true;
                    }
                }

                return false;
            }
        }
    }

    private VirusHolderComponent EnsureHolder(EntityUid host) => EnsureComp<VirusHolderComponent>(host);

    #endregion

    #region Suppression

    public void SuppressVirus(Entity<VirusComponent> virus, TimeSpan duration)
    {
        if (virus.Comp.SuppressedUntil == null)
        {
            foreach (var (symptomId, state) in virus.Comp.Symptoms)
                ApplyStage(virus, symptomId, state.Stage, -1);

            _adminLog.Add(LogType.Virology, LogImpact.Low,
                $"Virus {DescribeVirus(virus.Comp)} on {ToPrettyString(virus.Comp.Carrier):target} was suppressed for {duration.TotalMinutes:0} min");
        }

        virus.Comp.SuppressedUntil = _timing.CurTime + duration;
        Dirty(virus);
        RaiseContentsChanged(virus.Comp.Carrier);
    }

    public void ReactivateVirus(Entity<VirusComponent> virus)
    {
        if (virus.Comp.SuppressedUntil == null)
            return;

        virus.Comp.SuppressedUntil = null;
        var now = _timing.CurTime;
        foreach (var (symptomId, state) in virus.Comp.Symptoms)
        {
            state.LastEmote = now;
            state.EmoteDelay = TimeSpan.Zero;
            ApplyStage(virus, symptomId, -1, state.Stage);
        }

        Dirty(virus);
        RaiseContentsChanged(virus.Comp.Carrier);
    }

    #endregion

    #region Stages

    public void ApplyStage(Entity<VirusComponent> virus, ProtoId<VirusSymptomPrototype> symptomId, int oldStage, int newStage)
    {
        if (!_proto.Resolve(symptomId, out var symptom))
            return;

        var carrier = virus.Comp.Carrier;
        if (!Exists(carrier))
            return;

        var species = GetSpecies(carrier);
        var oldSet = BuildStageComponents(symptom, oldStage, species);
        var newSet = BuildStageComponents(symptom, newStage, species);
        var granted = virus.Comp.GrantedComponents.GetValueOrDefault(symptomId) ?? [];
        var othersHold = GetComponentsGrantedByOthers(carrier, virus.Owner, symptomId);

        foreach (var name in oldSet.Keys)
        {
            if (newSet.ContainsKey(name))
                continue;

            if (granted.Remove(name)
                && !othersHold.Contains(name)
                && _factory.TryGetRegistration(name, out var reg))
                RemComp(carrier, reg.Type);
        }

        var toAdd = new ComponentRegistry();
        foreach (var (name, entry) in newSet)
        {
            if (!_factory.TryGetRegistration(name, out var reg))
                continue;

            if (granted.Contains(name))
            {
                RemComp(carrier, reg.Type);
                granted.Remove(name);
            }
            else if (HasComp(carrier, reg.Type))
            {
                // if already present - co-own comp
                // or it's a trait/mob's own component - leave it
                if (othersHold.Contains(name))
                    granted.Add(name);
                continue;
            }

            toAdd[name] = entry;
            granted.Add(name);
        }

        if (toAdd.Count > 0)
            EntityManager.AddComponents(carrier, toAdd, removeExisting: false);

        if (granted.Count > 0)
            virus.Comp.GrantedComponents[symptomId] = granted;
        else
            virus.Comp.GrantedComponents.Remove(symptomId);
    }

    private HashSet<string> GetComponentsGrantedByOthers(EntityUid carrier, EntityUid exceptVirus, ProtoId<VirusSymptomPrototype>? exceptSymptom)
    {
        var held = new HashSet<string>();

        if (!TryComp<VirusHolderComponent>(carrier, out var holder))
            return held;

        foreach (var other in EnumerateStrains(holder))
        {
            foreach (var (symptomId, granted) in other.Comp.GrantedComponents)
            {
                if (other.Owner == exceptVirus && (exceptSymptom == null || exceptSymptom == symptomId))
                    continue;

                held.UnionWith(granted);
            }
        }

        return held;
    }

    public IVirusEffect[] BuildStageEffects(VirusSymptomPrototype symptom, int stage, EntityUid carrier)
    {
        if (stage < 0 || stage >= symptom.Stages.Length)
            return [];

        if (GetSpecies(carrier) is { } species && symptom.SpeciesOverrides.TryGetValue(species, out var over))
        {
            if (over.Immune || stage < over.MinStage || over.SuppressEffects)
                return [];
        }

        return symptom.Stages[stage].Effects;
    }

    public int ForceAdvanceAllSymptoms(EntityUid host)
    {
        var advanced = 0;
        foreach (var strain in EnumerateStrains(host))
        {
            var virus = strain.Comp;
            var advancedHere = 0;
            foreach (var (symptomId, state) in virus.Symptoms)
            {
                if (!_proto.Resolve(symptomId, out var symptom) || state.Stage + 1 >= symptom.Stages.Length)
                    continue;

                var oldStage = state.Stage;
                state.Stage++;
                state.StageStartTime = _timing.CurTime;
                ApplyStage(strain, symptomId, oldStage, state.Stage);

                var newStage = symptom.Stages[state.Stage];
                if (newStage.ProgressMessage is { } message)
                    VirusChat.SendSelfMessage(_chatManager, EntityManager, virus.Carrier, Loc.GetString(message), newStage.ProgressMessageColor);

                advancedHere++;
            }

            if (advancedHere > 0)
            {
                Dirty(strain);
                RaiseContentsChanged(virus.Carrier);
                advanced += advancedHere;
            }
        }

        return advanced;
    }

    private static ComponentRegistry BuildStageComponents(VirusSymptomPrototype symptom, int stage, ProtoId<SpeciesPrototype>? species)
    {
        if (stage < 0 || stage >= symptom.Stages.Length)
            return [];

        VirusSpeciesOverride? over = null;
        if (species is { } speciesId && symptom.SpeciesOverrides.TryGetValue(speciesId, out var found))
            over = found;

        if (over is { Immune: true })
            return [];

        if (over != null && stage < over.MinStage)
            return [];

        if (over?.ReplaceComponents is { } replace)
            return Copy(replace);

        var result = Copy(symptom.Stages[stage].Components);

        if (over != null)
        {
            foreach (var name in over.RemoveComponents)
                result.Remove(name);

            foreach (var (name, entry) in over.AddComponents)
                result[name] = entry;
        }

        return result;
    }

    private static ComponentRegistry Copy(ComponentRegistry source)
    {
        var copy = new ComponentRegistry();
        foreach (var (name, entry) in source)
            copy[name] = entry;

        return copy;
    }

    #endregion

    #region Identity + immunity

    /// <summary>Sorted symptom ids, used for immunity and re-infection checks. Cached per strain.</summary>
    public string GetIdentity(VirusComponent virus)
    {
        if (virus.CachedIdentity is { } cached)
            return cached;

        _identityBuf.Clear();
        foreach (var symptom in virus.Symptoms.Keys)
            _identityBuf.Add(symptom.Id);

        return virus.CachedIdentity = BuildIdentity();
    }

    public string GetIdentity(VirusDescriptor descriptor)
    {
        _identityBuf.Clear();
        foreach (var snapshot in descriptor.Symptoms)
            _identityBuf.Add(snapshot.Symptom.Id);

        return BuildIdentity();
    }

    /// <summary>Identity of supervirus that mwrge strain with incoming descriptor would produce.</summary>
    private string GetUnionIdentity(VirusComponent target, VirusDescriptor incoming)
    {
        _identityBuf.Clear();
        foreach (var symptom in target.Symptoms.Keys)
            _identityBuf.Add(symptom.Id);

        foreach (var snapshot in incoming.Symptoms)
        {
            if (_identityBuf.Count >= MaxSupervirusSymptoms)
                break;

            var id = snapshot.Symptom.Id;
            if (!_identityBuf.Contains(id))
                _identityBuf.Add(id);
        }

        return BuildIdentity();
    }

    private string BuildIdentity()
    {
        _identityBuf.Sort(StringComparer.Ordinal);
        return string.Join(',', _identityBuf);
    }

    /// <summary>Strain genome is set by first symptom.</summary>
    public VirusGenome GetGenome(List<ProtoId<VirusSymptomPrototype>> symptoms)
    {
        if (symptoms.Count > 0 && _proto.Resolve(symptoms[0], out var first))
            return first.Genome;

        return VirusGenome.Rna;
    }

    public ProtoId<ReagentPrototype>? RollAccelerant(IReadOnlySet<ProtoId<ReagentPrototype>>? exclude = null)
    {
        if (!_proto.Resolve<VirusCurePoolPrototype>(AccelerantPool, out var pool) || pool.Accelerants.Count == 0)
            return null;

        if (exclude is { Count: > 0 })
        {
            _accelerantBuf.Clear();
            foreach (var accelerant in pool.Accelerants)
            {
                if (!exclude.Contains(accelerant))
                    _accelerantBuf.Add(accelerant);
            }

            if (_accelerantBuf.Count > 0)
                return _random.Pick(_accelerantBuf);
        }

        return _random.Pick(pool.Accelerants);
    }

    /// <summary>Accelerants.</summary>
    public static HashSet<ProtoId<ReagentPrototype>> CollectAccelerants(VirusComponent virus, VirusSymptomState? except = null)
    {
        var used = new HashSet<ProtoId<ReagentPrototype>>();
        foreach (var state in virus.Symptoms.Values)
        {
            if (ReferenceEquals(state, except))
                continue;

            if (state.Accelerant is { } accelerant)
                used.Add(accelerant);
        }

        return used;
    }

    public static HashSet<ProtoId<ReagentPrototype>> CollectAccelerants(VirusDescriptor descriptor)
    {
        var used = new HashSet<ProtoId<ReagentPrototype>>();
        foreach (var snapshot in descriptor.Symptoms)
        {
            if (snapshot.Accelerant is { } accelerant)
                used.Add(accelerant);
        }

        return used;
    }

    /// <summary>Grants immunity to a strain identity. Returns true if it was newly granted.</summary>
    public bool AddImmunity(EntityUid host, string identity)
    {
        var immunities = EnsureComp<VirusImmunitiesComponent>(host);
        if (!immunities.Strains.Add(identity))
            return false;

        Dirty(host, immunities);
        return true;
    }

    public ProtoId<SpeciesPrototype>? GetSpecies(EntityUid uid)
    {
        if (TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return humanoid.Species;

        return null;
    }

    #endregion

    #region Symptom info

    public bool TryGetSymptomDescription(ProtoId<VirusSymptomPrototype> symptomId, out string? description)
    {
        description = null;
        if (!_proto.Resolve(symptomId, out var symptom))
            return false;

        foreach (var stage in symptom.Stages)
        {
            if (stage.Detection is not { } detection)
                continue;

            description = Loc.GetString(detection.Description);
            return true;
        }

        return false;
    }

    public string FormatSymptom(ProtoId<VirusSymptomPrototype> symptomId, string description, int stage, ProtoId<ReagentPrototype>? accelerant = null)
    {
        if (!_proto.Resolve(symptomId, out var symptom) || symptom.Stages.Length <= 1)
            return description;

        if (accelerant is { } acc && _proto.Resolve(acc, out var accProto))
        {
            return Loc.GetString("disease-diagnoser-symptom-stage",
                ("symptom", description),
                ("stage", stage + 1),
                ("max", symptom.Stages.Length),
                ("reagent", accProto.LocalizedName));
        }

        return Loc.GetString("pathology-symptom-stage",
            ("symptom", description),
            ("stage", stage + 1),
            ("max", symptom.Stages.Length));
    }

    public List<string> GetAnalyzerVirusLines(Entity<VirusHolderComponent?> entity)
    {
        var lines = new List<string>();
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return lines;

        if (entity.Comp.Viruses.Count > 0)
            lines.Add(Loc.GetString("health-analyzer-report-disease-detected"));

        return lines;
    }

    #endregion
}
