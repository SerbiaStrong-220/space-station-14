// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.Shared.Access.Systems;
using Content.Shared.SS220.CCVars;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Server.Maps;
using Content.Shared.Inventory;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
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

namespace Content.Server.SS220.Investigation;

/// <summary>
///     Drives the sampling loops that feed <see cref="InvestigationRecorder"/>.
/// </summary>
/// <remarks>
///     Three independent cadences, because the data changes at wildly different rates:
///     positions (fast, continuous), navmap (slow, event-driven), character loadouts (slow, bursty).
///     Sampling all three at position rate would multiply the bundle size for no investigative benefit.
/// </remarks>
public sealed class InvestigationRecorderSystem : EntitySystem, IInvestigationPositionSource
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
    private float _dirtyInterval;
    private int _storageDepth;

    private float _positionAccumulator;
    private float _navMapAccumulator;
    private float _characterAccumulator;
    private float _dirtyAccumulator;

    /// <summary>
    ///     Tracked entities whose loadout is known to have changed and have not been re-snapshotted yet.
    /// </summary>
    /// <remarks>
    ///     Equipment events are the trigger, but they are not acted on inline. Filling a backpack raises one per
    ///     item, and each would rebuild the entity's whole snapshot; worse, the events fire mid-transaction, so
    ///     the state read would not be the settled one. Coalescing into a set and draining it a moment later
    ///     costs one snapshot per entity per drain no matter how many events arrived.
    /// </remarks>
    private readonly HashSet<EntityUid> _dirtyCharacters = new();

    /// <summary>
    ///     How many characters the backstop sweep snapshots per tick. See <see cref="AdvanceCharacterSweep"/>.
    /// </summary>
    private const int SweepBatchSize = 4;

    /// <summary>
    ///     Roster entities still to be visited by the current backstop sweep, drained a few per tick so the sweep
    ///     never lands as one spike.
    /// </summary>
    private readonly Queue<EntityUid> _sweepQueue = new();

    /// <summary>
    ///     Inverse world matrix per grid, valid for the duration of one position sweep. See <see cref="SamplePositions"/>.
    /// </summary>
    private readonly Dictionary<EntityUid, Matrix3x2> _gridMatrices = new();

    /// <summary>
    ///     Job prototype id to its department and that department's colour.
    /// </summary>
    /// <remarks>
    ///     Built once instead of scanning every <see cref="DepartmentPrototype"/> and its role list per character
    ///     per snapshot, which is what this used to do. Rebuilt on prototype reload, which is the only thing that
    ///     can invalidate it.
    /// </remarks>
    private Dictionary<string, (string Department, string Color)>? _departmentsByJob;

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
    private EntityQuery<GhostComponent> _ghostQuery;

    public override void Initialize()
    {
        base.Initialize();

        _recorder.Initialize();
        _recorder.SetPositionSource(this);

        _xformQuery = GetEntityQuery<TransformComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();

        _cfg.OnValueChanged(CCVars220.InvestigationPositionInterval, interval => _positionInterval = interval, true);
        _cfg.OnValueChanged(CCVars220.InvestigationPositionEpsilon, epsilon => _positionEpsilon = epsilon, true);
        _cfg.OnValueChanged(CCVars220.InvestigationNavMapInterval, interval => _navMapInterval = interval, true);
        _cfg.OnValueChanged(CCVars220.InvestigationCharacterInterval, interval => _characterInterval = interval, true);
        _cfg.OnValueChanged(CCVars220.InvestigationDirtyInterval, interval => _dirtyInterval = interval, true);
        _cfg.OnValueChanged(CCVars220.InvestigationStorageDepth, depth => _storageDepth = depth, true);

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);

        // RoundEndedEvent only fires on the normal end-of-round path. An admin `restartround`, `golobby`, the
        // update-restart and the game rules that cut a round short all call GameTicker.RestartRound directly,
        // which raises this and nothing else. Without it those rounds would never stop recording, the next
        // StartRound would find a live session, and a whole shift would be appended to the previous round's
        // bundle under the previous round's id.
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        // Equipment changes are what make a loadout snapshot stale, so they drive re-sampling rather than the
        // ten second poll noticing after the fact. "Who was holding the weapon at 14:32" is the question this
        // stream exists to answer, and a ten second window is wide enough to miss a whole fight.
        //
        // All six are directed at the wearer or holder rather than at the item, so the subscription's own uid is
        // the character to re-snapshot. Filtered on MindContainerComponent purely to keep item-to-item traffic
        // out of the handler; roster membership is what actually decides, inside MarkCharacterDirty.
        SubscribeLocalEvent<MindContainerComponent, DidEquipEvent>((uid, _, _) => MarkCharacterDirty(uid));
        SubscribeLocalEvent<MindContainerComponent, DidUnequipEvent>((uid, _, _) => MarkCharacterDirty(uid));
        SubscribeLocalEvent<MindContainerComponent, DidEquipHandEvent>((uid, _, _) => MarkCharacterDirty(uid));
        SubscribeLocalEvent<MindContainerComponent, DidUnequipHandEvent>((uid, _, _) => MarkCharacterDirty(uid));

        // The job on a snapshot comes from the mind, so a mind arriving or leaving changes it too.
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>((uid, _, _) => MarkCharacterDirty(uid));
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>((uid, _, _) => MarkCharacterDirty(uid));

        _prototypes.PrototypesReloaded += _ => _departmentsByJob = null;
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
        _dirtyCharacters.Clear();
        _sweepQueue.Clear();
        _gridMatrices.Clear();
        _positionAccumulator = 0f;
        _navMapAccumulator = 0f;
        _characterAccumulator = 0f;
        _dirtyAccumulator = 0f;

        _recorder.StartRound(ev.RoundId, _gameMap.GetSelectedMap()?.MapName);
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        StopRecording(ev.RoundDuration);
    }

    /// <summary>
    ///     Backstop for rounds that are restarted without ending. Runs before the entities are flushed, so a
    ///     final sample is still meaningful.
    /// </summary>
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        // Already stopped on the normal path, where RoundEndedEvent came first.
        if (_recorder.IsRecording)
            StopRecording(null);
    }

    private void StopRecording(TimeSpan? duration)
    {
        // Take one final sample of everything so the bundle ends on a complete picture rather than mid-interval.
        // Deliberately not amortised the way the in-round sweep is: there is no next tick to spread it over, and
        // a hitch on the tick the round ends costs nothing anybody is playing through.
        if (_recorder.IsRecording)
        {
            SamplePositions();
            SampleNavMap();

            foreach (var uid in _recorder.Roster.Keys)
            {
                if (_metaQuery.HasComp(uid))
                    SampleCharacter(uid);
            }
        }

        _recorder.StopRound(duration);
    }

    /// <summary>
    ///     The first time an entity is ever player-controlled it joins the roster, and stays there for the rest of the
    ///     round. Tracking bodies after the player has left them is deliberate: a corpse being dragged off and stuffed
    ///     into a locker is exactly the kind of thing investigations turn on.
    /// </summary>
    /// <remarks>
    ///     Every attachment also writes a control row, including ones for entities already on the roster. The roster
    ///     records who an entity was; the control stream records who was driving it at a given moment, and those
    ///     stop being the same answer the moment a body is cloned, borged, revived by somebody else, or possessed
    ///     by an admin.
    /// </remarks>
    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!_recorder.IsRecording || IsUninterestingObserver(ev.Entity))
            return;

        if (!_recorder.Roster.ContainsKey(ev.Entity))
        {
            var name = _metaQuery.TryComp(ev.Entity, out var meta) ? meta.EntityName : "<unknown>";
            var prototype = meta?.EntityPrototype?.ID;

            _recorder.TrackEntity(ev.Entity, ev.Player.UserId.UserId, ev.Player.Name, name, prototype);
        }

        _recorder.WriteControl(ev.Entity, ev.Player.UserId.UserId, ev.Player.Name, attached: true);
    }

    /// <summary>
    ///     Closes the control row opened by <see cref="OnPlayerAttached"/>, so the bundle says when a body stopped
    ///     being driven as well as when it started.
    /// </summary>
    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        // Not gated on roster membership: an entity can only be detached from if it was attached to, and if it was
        // attached to while recording it is on the roster. Gating would only ever drop the closing half of a pair.
        if (!_recorder.IsRecording || IsUninterestingObserver(ev.Entity))
            return;

        _recorder.WriteControl(ev.Entity, ev.Player.UserId.UserId, ev.Player.Name, attached: false);
    }

    /// <summary>
    ///     Whether an entity is a pure observer whose movement carries no investigative meaning.
    /// </summary>
    /// <remarks>
    ///     Ghosts fly through walls at high speed, exist in large numbers late in a round, and cannot touch
    ///     anything, so sampling them costs a real share of the position stream to record nothing anybody would
    ///     ever ask about.
    ///
    ///     Keyed on <see cref="GhostComponent"/> specifically, not on "looks incorporeal". A revenant is
    ///     incorporeal and spectral but is a mob that acts on the station, and where it went is exactly the sort
    ///     of thing an investigation needs; it carries no <see cref="GhostComponent"/>, so it is kept. The same
    ///     goes for any other ghost-like antagonist built on <c>Incorporeal</c>.
    /// </remarks>
    private bool IsUninterestingObserver(EntityUid uid)
    {
        return _ghostQuery.HasComp(uid);
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
            BeginCharacterSweep();
        }

        if (_sweepQueue.Count > 0)
        {
            AdvanceCharacterSweep();
        }
        else
        {
            // Skipped while the backstop sweep is running, because that already covers everything outstanding.
            _dirtyAccumulator += frameTime;
            if (_dirtyAccumulator >= _dirtyInterval)
            {
                _dirtyAccumulator = 0f;
                DrainDirtyCharacters();
            }
        }

        _recorder.Update(frameTime);
    }

    #region Positions

    /// <inheritdoc/>
    /// <remarks>
    ///     Used for speech from entities that are not on the roster, which never get a sampled position.
    ///     Same container-aware resolution as <see cref="SamplePositions"/>, but without the per-sweep matrix
    ///     cache: this is called once per line of speech from an untracked entity, not in a loop.
    /// </remarks>
    public bool TryGetPosition(EntityUid uid, out EntityUid? grid, out Vector2 local, out EntityUid? container)
    {
        grid = null;
        local = default;
        container = null;

        if (!_xformQuery.TryComp(uid, out var xform))
            return false;

        if (_container.TryGetContainingContainer((uid, xform, null), out var containing))
            container = containing.Owner;

        grid = xform.GridUid;
        var worldPos = _transform.GetWorldPosition(uid);
        local = grid is { } gridUid
            ? Vector2.Transform(worldPos, _transform.GetInvWorldMatrix(gridUid))
            : worldPos;

        return true;
    }

    private void SamplePositions()
    {
        // Inverting a grid's world matrix is not free, and essentially everyone is standing on the same grid —
        // the station. Resolving it per entity meant ~one matrix inversion per player per sample for a value
        // that is identical across almost all of them. Cached for the duration of the sweep, which is exactly as
        // long as it stays valid: grids move between ticks, never within one.
        _gridMatrices.Clear();

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
                ? Vector2.Transform(worldPos, GetInvGridMatrix(gridUid))
                : worldPos;

            _recorder.WritePosition(uid, grid, local, container, _positionEpsilon);
            SampleHealth(uid);
        }
    }

    private Matrix3x2 GetInvGridMatrix(EntityUid grid)
    {
        if (_gridMatrices.TryGetValue(grid, out var cached))
            return cached;

        var matrix = _transform.GetInvWorldMatrix(grid);
        _gridMatrices[grid] = matrix;
        return matrix;
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
            // without any rendering at all. They change rarely, so they are only re-emitted when they change.
            var beacons = ComputeBeaconHash(navMap);
            if (firstSeen || _seenGrids[uid] != beacons)
            {
                _recorder.WriteNavMapBeacons(uid, navMap.Beacons.Values.Select(object (beacon) => new
                {
                    name = beacon.Text,
                    x = Math.Round(beacon.Position.X, 2),
                    y = Math.Round(beacon.Position.Y, 2),
                    color = beacon.Color.ToHex(),
                }));
            }

            _seenGrids[uid] = beacons;
        }
    }

    /// <summary>
    ///     Fingerprint of a grid's whole beacon set.
    /// </summary>
    /// <remarks>
    ///     This used to be the beacon count, which missed every change that kept the count the same: renaming a
    ///     room, recolouring a department, or moving a beacon after a remap. Each beacon row is the complete set
    ///     for that grid, so a missed change meant the reader kept labelling rooms by their old names for the
    ///     rest of the round.
    ///
    ///     Beacons live in a dictionary whose enumeration order is not guaranteed across mutations, so the
    ///     per-beacon hashes are combined with XOR rather than fed into a <see cref="HashCode"/> in sequence:
    ///     order-independent, which is what makes the comparison stable.
    /// </remarks>
    private static int ComputeBeaconHash(NavMapComponent navMap)
    {
        var combined = navMap.Beacons.Count;

        foreach (var beacon in navMap.Beacons.Values)
        {
            combined ^= HashCode.Combine(beacon.Text, beacon.Position, beacon.Color);
        }

        return combined;
    }

    #endregion

    #region Characters

    private Dictionary<string, (string Department, string Color)> DepartmentsByJob
    {
        get
        {
            if (_departmentsByJob is { } cached)
                return cached;

            var byJob = new Dictionary<string, (string, string)>();
            foreach (var department in _prototypes.EnumeratePrototypes<DepartmentPrototype>())
            {
                var color = department.Color.ToHex();
                foreach (var role in department.Roles)
                {
                    // First department wins, matching the old scan's `break` on first match.
                    byJob.TryAdd(role, (department.ID, color));
                }
            }

            _departmentsByJob = byJob;
            return byJob;
        }
    }

    /// <summary>
    ///     Queues every tracked character for re-snapshotting. The periodic backstop for anything no event
    ///     covers — surgery, admin verbs, gibbing, prototype hotswaps.
    /// </summary>
    /// <remarks>
    ///     Queued rather than done inline. A snapshot is not cheap — effective access resolved through the ID
    ///     slot and every PDA, the full inventory, both hands, and storage contents to
    ///     <c>investigation.storage_depth</c>, plus the collections each of those allocates — and doing the whole
    ///     roster at once put all of that on one tick. On a full server that is a spike proportional to the
    ///     player count landing every ten seconds, which is the shape of hitch players notice even when the
    ///     average cost is invisible.
    /// </remarks>
    private void BeginCharacterSweep()
    {
        // The previous sweep has not finished. Refilling now would clear the tail it never reached and restart
        // from the top of the roster, so the same entities would be sampled forever and the ones after them
        // never. Only reachable if the character interval is set shorter than a sweep takes to drain, but the
        // failure mode is silent and permanent, so it is worth the branch.
        if (_sweepQueue.Count > 0)
            return;

        foreach (var uid in _recorder.Roster.Keys)
        {
            _sweepQueue.Enqueue(uid);
        }

        // Everything on the roster is about to be sampled, so nothing is outstanding.
        _dirtyCharacters.Clear();
    }

    /// <summary>
    ///     Snapshots the next slice of the queued backstop sweep.
    /// </summary>
    /// <remarks>
    ///     Sized so the whole roster is covered well inside one character interval no matter how many players
    ///     are on: at the default 10s interval and 30 ticks per second, <see cref="SweepBatchSize"/> per tick
    ///     drains a 300-player roster in a tenth of the interval. The point is only to spread the work, not to
    ///     slow it down.
    /// </remarks>
    private void AdvanceCharacterSweep()
    {
        for (var sampled = 0; sampled < SweepBatchSize && _sweepQueue.TryDequeue(out var uid); sampled++)
        {
            // Entities die between the sweep being queued and reaching them; the roster keeps the entry either
            // way, but there is nothing left to snapshot.
            if (_metaQuery.HasComp(uid))
                SampleCharacter(uid);
        }
    }

    /// <summary>
    ///     Re-snapshots only the characters whose equipment changed since the last drain.
    /// </summary>
    /// <remarks>
    ///     This is what keeps loadout records current without paying for the full roster. In a quiet second the
    ///     set is empty and this costs a branch; in a busy one it costs a handful of snapshots instead of every
    ///     player's.
    /// </remarks>
    private void DrainDirtyCharacters()
    {
        if (_dirtyCharacters.Count == 0)
            return;

        foreach (var uid in _dirtyCharacters)
        {
            if (_metaQuery.HasComp(uid))
                SampleCharacter(uid);
        }

        _dirtyCharacters.Clear();
    }

    private void SampleCharacter(EntityUid uid)
    {
        var snapshot = BuildCharacterSnapshot(uid, out var fingerprint);
        _recorder.WriteCharacterIfChanged(uid, snapshot, fingerprint);
    }

    /// <summary>
    ///     Marks a tracked entity for re-snapshotting on the next drain.
    /// </summary>
    /// <remarks>
    ///     Deliberately does no work of its own: this runs inside equipment events, on the hot path of every
    ///     player picking anything up, so it must stay a roster lookup and a set insert.
    /// </remarks>
    private void MarkCharacterDirty(EntityUid uid)
    {
        if (_recorder.IsRecording && _recorder.Roster.ContainsKey(uid))
            _dirtyCharacters.Add(uid);
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
        if (job != null && DepartmentsByJob.TryGetValue(job, out var jobDepartment))
        {
            department = jobDepartment.Department;
            departmentColor = jobDepartment.Color;
        }

        var access = _accessReader.FindAccessTags(uid)
            .Select(tag => tag.Id)
            .OrderBy(tag => tag, StringComparer.Ordinal)
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

            // Sorted here, once, rather than inside the fingerprint. The fingerprint has to be order-independent
            // or shuffling a bag would look like a loadout change, and doing it here means the written row is
            // deterministic too — the same bag always serialises the same way.
            carried.Sort(StringComparer.Ordinal);
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

        // Slots come out of the inventory template in a fixed order and access and carried are both sorted by
        // their callers, so nothing here needs to re-sort to be stable.
        foreach (var (slot, item) in worn)
        {
            hash.Add(slot);
            hash.Add(item);
        }

        foreach (var item in held)
            hash.Add(item);

        foreach (var item in carried)
            hash.Add(item);

        return hash.ToHashCode();
    }

    #endregion
}
