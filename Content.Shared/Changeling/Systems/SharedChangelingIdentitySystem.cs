// SS220 Changeling
using System.Linq;
using System.Numerics;
using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Systems;

public abstract class SharedChangelingIdentitySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    [Dependency] private readonly SharedCloningSystem _cloningSystem = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverrideSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingIdentityComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ChangelingIdentityComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ChangelingStoredIdentityComponent, ComponentRemove>(OnStoredRemove);

        SubscribeLocalEvent<ChangelingDevouredComponent, ComponentShutdown>(OnDevouredShutdown);
        SubscribeLocalEvent<ChangelingDevouredComponent, CloningAttemptEvent>(OnDevouredCloningAttempt);
    }

    private void OnPlayerAttached(Entity<ChangelingIdentityComponent> ent, ref PlayerAttachedEvent args)
    {
        HandOverPvsOverride(ent, args.Player);
    }

    private void OnPlayerDetached(Entity<ChangelingIdentityComponent> ent, ref PlayerDetachedEvent args)
    {
        CleanupPvsOverride(ent, args.Player);
    }

    private void OnMapInit(Entity<ChangelingIdentityComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient || ent.Comp.IdentityInitialized)
            return;

        ent.Comp.IdentityInitialized = true;

        // Make a backup of our current identity so we can transform back.
        var clone = TryStoreIdentity(ent, ent.Owner, countForObjective: false, out var stored)
            ? stored
            : null;
        ent.Comp.CurrentIdentity = clone;
        ent.Comp.CurrentGenome = GetGenomeId(ent.Owner);
        Dirty(ent);
    }

    private void OnDevouredCloningAttempt(Entity<ChangelingDevouredComponent> ent, ref CloningAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnShutdown(Entity<ChangelingIdentityComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<ActorComponent>(ent, out var actor))
            CleanupPvsOverride(ent, actor.PlayerSession);

        if (ent.Comp.SuppressShutdownCleanup)
            return;

        CleanupChangelingNullspaceIdentities(ent);
        CleanupDevouredReferences(ent);
    }

    /// <summary>
    /// Moves all persistent changeling identity state to a new physical body without deleting the paused
    /// identity clones. Used by body swap and Last Resort.
    /// </summary>
    public bool TryTransferIdentity(Entity<ChangelingIdentityComponent?> source, EntityUid target)
    {
        if (!Resolve(source, ref source.Comp, false) ||
            TerminatingOrDeleted(target) ||
            HasComp<ChangelingIdentityComponent>(target))
            return false;

        // Construct the component fully before adding it. Adding a component to a map-initialized entity raises
        // MapInit synchronously; IdentityInitialized tells that handler this is transferred state, not a new changeling.
        var targetIdentity = new ChangelingIdentityComponent
        {
            IdentityInitialized = true,
            MaxStoredIdentities = source.Comp.MaxStoredIdentities,
            ConsumedIdentities = new Dictionary<EntityUid, EntityUid?>(source.Comp.ConsumedIdentities),
            StoredIdentities = new HashSet<EntityUid>(source.Comp.StoredIdentities),
            StoredGenomes = new Dictionary<EntityUid, string>(source.Comp.StoredGenomes),
            AbsorbedGenomes = new HashSet<string>(source.Comp.AbsorbedGenomes),
            CurrentIdentity = source.Comp.CurrentIdentity,
            CurrentGenome = source.Comp.CurrentGenome,
            IdentityCloningSettings = source.Comp.IdentityCloningSettings,
        };
        AddComp(target, targetIdentity);

        foreach (var stored in targetIdentity.StoredIdentities)
        {
            if (TryComp<ChangelingStoredIdentityComponent>(stored, out var storedIdentity))
                storedIdentity.ChangelingOwner = target;
        }

        // Devoured bodies remember the changeling that consumed them. Point those records at the new body
        // so duplicate-devour checks and later cleanup continue to work.
        var devouredQuery = EntityQueryEnumerator<ChangelingDevouredComponent>();
        while (devouredQuery.MoveNext(out _, out var devoured))
        {
            if (!devoured.DevouredBy.Remove(source.Owner))
                continue;

            devoured.DevouredBy.Add(target);
        }

        source.Comp.SuppressShutdownCleanup = true;
        RemComp<ChangelingIdentityComponent>(source.Owner);
        return true;
    }

    // Set all references to this entity to null to prevent PVS errors when networking.
    private void OnDevouredShutdown(Entity<ChangelingDevouredComponent> ent, ref ComponentShutdown args)
    {
        foreach (var ling in ent.Comp.DevouredBy)
        {
            if (!TryComp<ChangelingIdentityComponent>(ling, out var identityComp))
                continue;

            var keysToUpdate = identityComp.ConsumedIdentities
                .Where(kvp => kvp.Value == ent.Owner)
                .Select(kvp => kvp.Key)
                .ToList();

            if (keysToUpdate.Count == 0)
                continue; // No need to dirty.

            foreach (var key in keysToUpdate)
                identityComp.ConsumedIdentities[key] = null;

            Dirty(ling, identityComp);
        }
    }

    private void OnStoredRemove(Entity<ChangelingStoredIdentityComponent> ent, ref ComponentRemove args)
    {
        if (_net.IsServer &&
            ent.Comp.ChangelingOwner is { } owner &&
            TryComp<ChangelingIdentityComponent>(owner, out var identity))
        {
            var removed = identity.ConsumedIdentities.Remove(ent.Owner);
            removed |= identity.StoredIdentities.Remove(ent.Owner);
            removed |= identity.StoredGenomes.Remove(ent.Owner);
            if (removed && identity.LifeStage < ComponentLifeStage.Stopping)
                Dirty(owner, identity);
        }

        // The last stored identity is being deleted, so the shared storage map can be cleaned up.
        if (_net.IsClient || Count<ChangelingStoredIdentityComponent>() > 1)
            return;

        var maps = AllEntityQuery<ChangelingIdentityStorageMapComponent, MapComponent>();
        while (maps.MoveNext(out var mapUid, out _, out var map))
            _map.QueueDeleteMap(map.MapId);
    }

    /// <summary>
    /// Cleanup all nullspaced Identities when the changeling no longer exists
    /// </summary>
    /// <param name="ent">the changeling</param>
    public void CleanupChangelingNullspaceIdentities(Entity<ChangelingIdentityComponent> ent)
    {
        if (_net.IsClient)
            return;

        foreach (var consumedIdentity in ent.Comp.ConsumedIdentities)
        {
            QueueDel(consumedIdentity.Key);
        }
    }

    /// <summary>
    /// Removes all references to the owning changeling from ChangelingDevouredComponents.
    /// </summary>
    /// <param name="ent">The changeling entity</param>
    private void CleanupDevouredReferences(Entity<ChangelingIdentityComponent> ent)
    {
        foreach (var devouredUid in ent.Comp.ConsumedIdentities.Values)
        {
            if (!TryComp<ChangelingDevouredComponent>(devouredUid, out var devouredComp))
                continue;

            devouredComp.DevouredBy.Remove(ent.Owner);
        }
    }

    /// <summary>
    /// Clone a target humanoid to a paused map.
    /// It creates a perfect copy of the target and can be used to pull components down for future use.
    /// </summary>
    /// <param name="settings">The settings to use for cloning.</param>
    /// <param name="target">The target to clone.</param>
    public EntityUid? CloneToPausedMap(CloningSettingsPrototype settings, EntityUid target)
    {
        // Don't create client side duplicate clones or a clientside map.
        if (_net.IsClient)
            return null;

        if (!TryComp<HumanoidProfileComponent>(target, out var humanoid)
            || !_prototype.Resolve(humanoid.Species, out var speciesPrototype))
            return null;

        var storageMap = EnsurePausedMap();
        var clone = Spawn(speciesPrototype.Prototype, new MapCoordinates(Vector2.Zero, storageMap));

        var storedIdentity = EnsureComp<ChangelingStoredIdentityComponent>(clone);
        storedIdentity.OriginalEntity = target; // TODO: network this once we have WeakEntityReference or the autonetworking source gen is fixed

        if (TryComp<ActorComponent>(target, out var actor))
            storedIdentity.OriginalSession = actor.PlayerSession;

        _visualBody.CopyAppearanceFrom(target, clone);
        _cloningSystem.CloneComponents(target, clone, settings);

        var targetName = _nameMod.GetBaseName(target);
        _metaSystem.SetEntityName(clone, targetName);

        return clone;
    }

    /// <summary>
    /// Clone a target humanoid to a paused map and add it to the Changelings list of identities.
    /// It creates a perfect copy of the target and can be used to pull components down for future use.
    /// </summary>
    /// <param name="ent">The Changeling.</param>
    /// <param name="target">The target to clone.</param>
    public EntityUid? CloneToPausedMap(Entity<ChangelingIdentityComponent> ent, EntityUid target)
    {
        return TryStoreIdentity(ent, target, countForObjective: true, out var clone)
            ? clone
            : null;
    }

    /// <summary>
    /// Attempts to create and store a usable identity sample. Storage is capped and never contains two
    /// active copies of the same genome. A genome can be acquired again after its sample is consumed.
    /// </summary>
    public bool TryStoreIdentity(
        Entity<ChangelingIdentityComponent> ent,
        EntityUid target,
        bool countForObjective,
        out EntityUid? clone)
    {
        var genome = GetGenomeId(target);
        if (genome == null)
        {
            clone = null;
            return false;
        }

        return TryStoreIdentity(ent, target, genome, target, countForObjective, out clone);
    }

    /// <summary>
    /// Stores a clone using an explicitly supplied genome id. This is used for hive downloads, where the
    /// paused identity clone is the appearance source but must retain the original victim's DNA identity.
    /// </summary>
    public bool TryStoreIdentity(
        Entity<ChangelingIdentityComponent> ent,
        EntityUid appearanceSource,
        string genome,
        EntityUid? originalEntity,
        bool countForObjective,
        out EntityUid? clone)
    {
        clone = null;
        if (string.IsNullOrWhiteSpace(genome) || ent.Comp.StoredGenomes.ContainsValue(genome))
            return false;

        if (ent.Comp.ConsumedIdentities.Count >= ent.Comp.MaxStoredIdentities)
            return false;

        if (!_prototype.Resolve(ent.Comp.IdentityCloningSettings, out var settings))
            return false;

        clone = CloneToPausedMap(settings, appearanceSource);
        if (clone == null)
            return false;

        Comp<ChangelingStoredIdentityComponent>(clone.Value).ChangelingOwner = ent.Owner;

        ent.Comp.ConsumedIdentities.Add(clone.Value, originalEntity);
        ent.Comp.StoredIdentities.Add(clone.Value);
        ent.Comp.StoredGenomes.Add(clone.Value, genome);

        if (countForObjective)
            RecordAbsorbedGenome(ent, genome, originalEntity ?? appearanceSource);
        Dirty(ent);
        HandlePvsOverride(ent, clone.Value);

        if (!countForObjective)
        {
            var acquired = new ChangelingGenomeAcquiredEvent(genome, originalEntity ?? appearanceSource, false, false);
            RaiseLocalEvent(ent.Owner, ref acquired);
        }
        return true;
    }

    /// <summary>
    /// Adds a genome to permanent round progress and raises the objective-facing acquisition event once.
    /// </summary>
    public bool RecordAbsorbedGenome(Entity<ChangelingIdentityComponent> ent, string genome, EntityUid source)
    {
        if (!ent.Comp.AbsorbedGenomes.Add(genome))
            return false;

        Dirty(ent);
        var acquired = new ChangelingGenomeAcquiredEvent(genome, source, true, true);
        RaiseLocalEvent(ent.Owner, ref acquired);
        return true;
    }

    /// <summary>
    /// Returns a stable genome identifier for a compatible humanoid.
    /// </summary>
    public string? GetGenomeId(EntityUid target)
    {
        if (!HasComp<HumanoidProfileComponent>(target))
            return null;

        if (TryComp<DnaComponent>(target, out var dna) && !string.IsNullOrWhiteSpace(dna.DNA))
            return dna.DNA;

        // Test humanoids and unusual species may omit forensic DNA.
        return $"entity:{target.Id}";
    }

    public bool HasStoredGenome(Entity<ChangelingIdentityComponent?> ent, string genome)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.StoredGenomes.ContainsValue(genome);
    }

    /// <summary>
    /// Attempts to drop an identity owned by this changeling from storage.
    /// </summary>
    public bool TryDropStoredIdentity(Entity<ChangelingIdentityComponent?> ent, EntityUid identity)
    {
        if (!Resolve(ent, ref ent.Comp, false) ||
            ent.Comp.CurrentIdentity == identity ||
            !ent.Comp.StoredIdentities.Contains(identity) ||
            !HasComp<ChangelingStoredIdentityComponent>(identity))
        {
            return false;
        }

        PredictedQueueDel(identity);
        var removed = ent.Comp.ConsumedIdentities.Remove(identity);
        removed |= ent.Comp.StoredIdentities.Remove(identity);
        removed |= ent.Comp.StoredGenomes.Remove(identity);
        if (removed)
            Dirty(ent);

        return removed;
    }

    /// <summary>
    /// Simple helper to add a PVS override to a nullspace identity.
    /// </summary>
    /// <param name="uid">The actor that should get the override.</param>
    /// <param name="identity">The identity stored in nullspace.</param>
    private void HandlePvsOverride(EntityUid uid, EntityUid identity)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _pvsOverrideSystem.AddSessionOverride(identity, actor.PlayerSession);
    }

    /// <summary>
    /// Cleanup all PVS overrides for the owner of the ChangelingIdentity
    /// </summary>
    /// <param name="ent">The changeling storing the identities.</param>
    /// <param name="session">The session you wish to remove the overrides from.</param>
    private void CleanupPvsOverride(Entity<ChangelingIdentityComponent> ent, ICommonSession session)
    {
        foreach (var identity in ent.Comp.ConsumedIdentities)
        {
            _pvsOverrideSystem.RemoveSessionOverride(identity.Key, session);
        }
    }

    /// <summary>
    /// Inform another session of the entities stored for transformation.
    /// </summary>
    /// <param name="ent">The changeling storing the identities.</param>
    /// <param name="session">The session you wish to inform.</param>
    public void HandOverPvsOverride(Entity<ChangelingIdentityComponent> ent, ICommonSession session)
    {
        foreach (var identity in ent.Comp.ConsumedIdentities)
        {
            _pvsOverrideSystem.AddSessionOverride(identity.Key, session);
        }
    }

    /// <summary>
    /// Create a paused map for storing devoured identities as a clone of the player.
    /// </summary>
    private MapId EnsurePausedMap()
    {
        var maps = AllEntityQuery<ChangelingIdentityStorageMapComponent, MapComponent>();
        if (maps.MoveNext(out _, out _, out var existingMap))
            return existingMap.MapId;

        var mapUid = _map.CreateMap(out var newMapId);
        AddComp<ChangelingIdentityStorageMapComponent>(mapUid);
        _metaSystem.SetEntityName(mapUid, Loc.GetString("changeling-paused-map-name"));
        _map.SetPaused(mapUid, true);
        return newMapId;
    }
}
