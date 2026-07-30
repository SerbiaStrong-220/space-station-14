using System.Linq;
using System.Numerics;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Server.Maps;
using Content.Shared.Inventory;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Storage;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Investigation;

/// <summary>
///     Drives the sampling loops that feed <see cref="InvestigationRecorder"/>.
/// </summary>
/// <remarks>
///     Three independent cadences, because the data changes at wildly different rates:
///     positions (fast, continuous), navmap (slow, event-driven), character loadouts (slow, bursty).
///     Sampling all three at position rate would multiply the bundle size for no investigative benefit.
/// </remarks>
public sealed class InvestigationRecorderSystem : EntitySystem
{
    [Dependency] private readonly InvestigationRecorder _recorder = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly IGameMapManager _gameMap = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private float _positionInterval;
    private float _positionEpsilon;
    private float _navMapInterval;
    private float _characterInterval;
    private int _storageDepth;

    private float _positionAccumulator;
    private float _navMapAccumulator;
    private float _characterAccumulator;

    /// <summary>
    ///     Tick of the last navmap sweep. Chunks dirtied at or after this are re-emitted.
    /// </summary>
    private GameTick _lastNavMapTick = GameTick.Zero;

    /// <summary>
    ///     Grids we have already emitted a full navmap snapshot for, plus their last known beacon count.
    /// </summary>
    private readonly Dictionary<EntityUid, int> _seenGrids = new();

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;

    public override void Initialize()
    {
        base.Initialize();

        _recorder.Initialize();
        _recorder.PositionResolver = ResolvePosition;

        _xformQuery = GetEntityQuery<TransformComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();

        _cfg.OnValueChanged(CCVars.InvestigationPositionInterval, v => _positionInterval = v, true);
        _cfg.OnValueChanged(CCVars.InvestigationPositionEpsilon, v => _positionEpsilon = v, true);
        _cfg.OnValueChanged(CCVars.InvestigationNavMapInterval, v => _navMapInterval = v, true);
        _cfg.OnValueChanged(CCVars.InvestigationCharacterInterval, v => _characterInterval = v, true);
        _cfg.OnValueChanged(CCVars.InvestigationStorageDepth, v => _storageDepth = v, true);

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _recorder.Shutdown();
    }

    #region Round lifecycle

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        _lastNavMapTick = GameTick.Zero;
        _seenGrids.Clear();
        _positionAccumulator = 0f;
        _navMapAccumulator = 0f;
        _characterAccumulator = 0f;

        _recorder.StartRound(ev.RoundId, _gameMap.GetSelectedMap()?.MapName);
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        // Take one final sample of everything so the bundle ends on a complete picture rather than mid-interval.
        if (_recorder.IsRecording)
        {
            SamplePositions();
            SampleNavMap();
            SampleCharacters();
        }

        _recorder.StopRound(ev.RoundDuration);
    }

    /// <summary>
    ///     The first time an entity is ever player-controlled it joins the roster, and stays there for the rest of the
    ///     round. Tracking bodies after the player has left them is deliberate: a corpse being dragged off and stuffed
    ///     into a locker is exactly the kind of thing investigations turn on.
    /// </summary>
    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!_recorder.IsRecording || _recorder.Roster.ContainsKey(ev.Entity))
            return;

        var name = _metaQuery.TryComp(ev.Entity, out var meta) ? meta.EntityName : "<unknown>";
        var prototype = meta?.EntityPrototype?.ID;

        _recorder.TrackEntity(ev.Entity, ev.Player.UserId.UserId, ev.Player.Name, name, prototype);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_recorder.IsRecording)
            return;

        _positionAccumulator += frameTime;
        if (_positionAccumulator >= _positionInterval)
        {
            _positionAccumulator = 0f;
            SamplePositions();
        }

        _navMapAccumulator += frameTime;
        if (_navMapAccumulator >= _navMapInterval)
        {
            _navMapAccumulator = 0f;
            SampleNavMap();
        }

        _characterAccumulator += frameTime;
        if (_characterAccumulator >= _characterInterval)
        {
            _characterAccumulator = 0f;
            SampleCharacters();
        }

        _recorder.Update(frameTime);
    }

    #region Positions

    /// <summary>
    ///     Grid-local position of any entity, resolved on demand.
    /// </summary>
    /// <remarks>
    ///     Used for speech from entities that are not on the roster, which never get a sampled position.
    ///     Same container-aware resolution as <see cref="SamplePositions"/>.
    /// </remarks>
    private (EntityUid? Grid, Vector2 Local, EntityUid? Container)? ResolvePosition(EntityUid uid)
    {
        if (!_xformQuery.TryComp(uid, out var xform))
            return null;

        EntityUid? container = null;
        if (_container.TryGetContainingContainer((uid, xform, null), out var containing))
            container = containing.Owner;

        var grid = xform.GridUid;
        var worldPos = _transform.GetWorldPosition(uid);
        var local = grid is { } gridUid
            ? Vector2.Transform(worldPos, _transform.GetInvWorldMatrix(gridUid))
            : worldPos;

        return (grid, local, container);
    }

    private void SamplePositions()
    {
        foreach (var uid in _recorder.Roster.Keys)
        {
            if (!_xformQuery.TryComp(uid, out var xform))
            {
                // Entity is gone. The roster entry stays as the record of who it was; only live caches are dropped.
                _recorder.UntrackEntity(uid);
                continue;
            }

            // Resolve through containers: an entity inside a locker or a bag reports the *container's* world
            // position, which is what we want on the map, but we also record which container it is in so the
            // reader can render "inside X" rather than a bare dot.
            EntityUid? container = null;
            if (_container.TryGetContainingContainer((uid, xform, null), out var containing))
                container = containing.Owner;

            var grid = xform.GridUid;
            var worldPos = _transform.GetWorldPosition(uid);

            // Grid-local coordinates are stable under grid motion, so a body on a moving shuttle does not smear
            // across the map. Entities off-grid (in space, on the map directly) fall back to world coordinates.
            var local = grid is { } gridUid
                ? Vector2.Transform(worldPos, _transform.GetInvWorldMatrix(gridUid))
                : worldPos;

            _recorder.WritePosition(uid, grid, local, container, _positionEpsilon);
            SampleHealth(uid);
        }
    }

    /// <summary>
    ///     Records total damage, mob state and the crit/dead thresholds for one entity.
    /// </summary>
    /// <remarks>
    ///     Sampled here rather than in the slow character loop because health during a fight changes in
    ///     well under a second, and a ten second snapshot would miss the entire fight. The thresholds ride
    ///     along so a reader can render a bar as a fraction rather than an opaque number.
    /// </remarks>
    private void SampleHealth(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        // GetTotalDamage is marked obsolete because content should not generally reduce damage to one
        // number. An investigation tool is the exception: a health bar is exactly that reduction, and the
        // per-type breakdown is already available from the admin log damage events.
#pragma warning disable CS0618
        var total = (float) _damageable.GetTotalDamage((uid, damageable));
#pragma warning restore CS0618

        // Copied to a local first: MobStateComponent is [Access]-restricted to read-only for us, and
        // calling ToString straight off the member counts as an execute access to the analyzer.
        var state = "Unknown";
        if (TryComp<MobStateComponent>(uid, out var mobState))
        {
            var current = mobState.CurrentState;
            state = current.ToString();
        }

        float? crit = _thresholds.TryGetIncapThreshold(uid, out var incap) ? (float) incap.Value : null;
        float? dead = _thresholds.TryGetDeadThreshold(uid, out var deadAt) ? (float) deadAt.Value : null;

        _recorder.WriteHealthIfChanged(uid, total, state, crit, dead);
    }

    #endregion

    #region NavMap

    private void SampleNavMap()
    {
        var sweepTick = _lastNavMapTick;
        _lastNavMapTick = _timing.CurTick;

        var query = EntityQueryEnumerator<NavMapComponent, MapGridComponent>();
        while (query.MoveNext(out var uid, out var navMap, out _))
        {
            var firstSeen = !_seenGrids.ContainsKey(uid);

            foreach (var (origin, chunk) in navMap.Chunks)
            {
                // On first sight of a grid emit every chunk (full snapshot); afterwards only what changed.
                if (!firstSeen && chunk.LastUpdate < sweepTick)
                    continue;

                _recorder.WriteNavMapChunk(uid, origin, chunk.TileData);
            }

            // Beacons are the station's room labels, which is what lets the reader answer "which room was this"
            // without any rendering at all. They change rarely, so only re-emit when the set size shifts.
            if (firstSeen || _seenGrids[uid] != navMap.Beacons.Count)
            {
                _recorder.WriteNavMapBeacons(uid, navMap.Beacons.Values.Select(object (b) => new
                {
                    name = b.Text,
                    x = Math.Round(b.Position.X, 2),
                    y = Math.Round(b.Position.Y, 2),
                    color = b.Color.ToHex(),
                }));
            }

            _seenGrids[uid] = navMap.Beacons.Count;
        }
    }

    #endregion

    #region Characters

    private void SampleCharacters()
    {
        foreach (var uid in _recorder.Roster.Keys)
        {
            if (!_metaQuery.HasComp(uid))
                continue;

            var snapshot = BuildCharacterSnapshot(uid, out var fingerprint);
            _recorder.WriteCharacterIfChanged(uid, snapshot, fingerprint);
        }
    }

    private object BuildCharacterSnapshot(EntityUid uid, out int fingerprint)
    {
        string? species = null;
        string? gender = null;
        var age = 0;

        if (TryComp<HumanoidProfileComponent>(uid, out var profile))
        {
            // Copied to locals first: the component is [Access]-restricted to read-only for us, and calling methods
            // straight off the members counts as an execute access to the analyzer.
            var speciesProto = profile.Species;
            var profileGender = profile.Gender;

            species = speciesProto.Id;
            gender = profileGender.ToString();
            age = profile.Age;
        }

        string? job = null;
        if (_mind.TryGetMind(uid, out var mindId, out _) && _jobs.MindTryGetJobId(mindId, out var jobProto))
            job = jobProto?.Id;

        // Department and its canonical colour are resolved here so the reader does not have to
        // reimplement the job-to-department mapping or invent its own palette.
        string? department = null;
        string? departmentColor = null;
        if (job != null)
        {
            foreach (var dept in _prototypes.EnumeratePrototypes<DepartmentPrototype>())
            {
                if (!dept.Roles.Contains(job))
                    continue;

                department = dept.ID;
                var colour = dept.Color;
                departmentColor = colour.ToHex();
                break;
            }
        }

        var access = _accessReader.FindAccessTags(uid)
            .Select(a => a.Id)
            .OrderBy(a => a, StringComparer.Ordinal)
            .ToList();

        var worn = new Dictionary<string, string>();
        if (TryComp<InventoryComponent>(uid, out var inventory))
        {
            var slots = _inventory.GetSlotEnumerator((uid, inventory));
            while (slots.MoveNext(out var container))
            {
                if (container.ContainedEntity is not { } item)
                    continue;

                worn[container.ID] = DescribeItem(item);
            }
        }

        var held = _hands.EnumerateHeld(uid)
            .Select(DescribeItem)
            .ToList();

        // Carried contents matter for "did they actually have the weapon they claim they didn't". Depth is capped
        // so a bag of bags cannot blow up the snapshot.
        var carried = new List<string>();
        if (_storageDepth > 0)
        {
            if (inventory != null)
            {
                var slots = _inventory.GetSlotEnumerator((uid, inventory));
                while (slots.MoveNext(out var container))
                {
                    if (container.ContainedEntity is { } item)
                        CollectStorage(item, _storageDepth, carried);
                }
            }

            foreach (var item in _hands.EnumerateHeld(uid))
            {
                CollectStorage(item, _storageDepth, carried);
            }
        }

        var name = _metaQuery.TryComp(uid, out var meta) ? meta.EntityName : null;

        fingerprint = ComputeFingerprint(species, job, name, access, worn, held, carried);

        return new
        {
            t = _timing.CurTick.Value,
            e = uid.Id,
            name,
            species,
            gender,
            age,
            job,
            department,
            departmentColor,
            access,
            worn,
            hands = held,
            carried,
        };
    }

    private void CollectStorage(EntityUid container, int depth, List<string> into)
    {
        if (depth <= 0 || !TryComp<StorageComponent>(container, out var storage))
            return;

        foreach (var item in storage.Container.ContainedEntities)
        {
            into.Add(DescribeItem(item));
            CollectStorage(item, depth - 1, into);
        }
    }

    private string DescribeItem(EntityUid item)
    {
        if (!_metaQuery.TryComp(item, out var meta))
            return "<unknown>";

        return meta.EntityPrototype?.ID ?? meta.EntityName;
    }

    private static int ComputeFingerprint(
        string? species,
        string? job,
        string? name,
        List<string> access,
        Dictionary<string, string> worn,
        List<string> held,
        List<string> carried)
    {
        var hash = new HashCode();
        hash.Add(species);
        hash.Add(job);
        hash.Add(name);

        foreach (var tag in access)
            hash.Add(tag);

        foreach (var (slot, item) in worn.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            hash.Add(slot);
            hash.Add(item);
        }

        foreach (var item in held)
            hash.Add(item);

        foreach (var item in carried.OrderBy(i => i, StringComparer.Ordinal))
            hash.Add(item);

        return hash.ToHashCode();
    }

    #endregion
}
