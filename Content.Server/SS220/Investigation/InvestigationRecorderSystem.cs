// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
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
using Content.Shared.Objectives.Systems;
using Content.Server.GameTicking;
using Content.Shared.Storage;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Investigation;

/// <summary>Drives the sampling loops that feed <see cref="InvestigationRecorder"/>.</summary>
/// <remarks>Three cadences, because positions, navmap and loadouts change at wildly different rates.</remarks>
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
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

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

    /// <summary>Tracked entities whose loadout changed and have not been re-snapshotted yet.</summary>
    /// <remarks>Coalesced rather than acted on inline: equipment events fire per item and mid-transaction.</remarks>
    private readonly HashSet<EntityUid> _dirtyCharacters = new();

    /// <summary>How many characters the backstop sweep snapshots per tick.</summary>
    private const int SweepBatchSize = 4;

    /// <summary>Roster entities still to be visited by the current sweep, drained a few per tick.</summary>
    private readonly Queue<EntityUid> _sweepQueue = new();

    /// <summary>Inverse world matrix per grid, valid for the duration of one position sweep.</summary>
    private readonly Dictionary<EntityUid, Matrix3x2> _gridMatrices = new();

    /// <summary>Job prototype id to its department and that department's colour.</summary>
    /// <remarks>Built once rather than scanned per character per snapshot. Rebuilt on prototype reload.</remarks>
    private Dictionary<string, (string Department, string Color)>? _departmentsByJob;

    /// <summary>Tick of the last navmap sweep. Chunks dirtied at or after this are re-emitted.</summary>
    private GameTick _lastNavMapTick = GameTick.Zero;

    /// <summary>Grids already emitted a full navmap snapshot for, plus their last known beacon count.</summary>
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

        // RoundEndedEvent only fires on the normal path. `restartround`, `golobby`, the update-restart and
        // rules that cut a round short call RestartRound directly, and would otherwise never stop recording.
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        // Equipment changes drive re-sampling rather than the ten second poll noticing after the fact.
        // All six are directed at the wearer or holder, so the subscription's own uid is the character.
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

        // Before StartRound, because that writes the first meta.json.
        RefreshGamemode();

        _recorder.StartRound(ev.RoundId, _gameMap.GetSelectedMap()?.MapName);
    }

    /// <summary>Copies the current game preset into the recorder.</summary>
    /// <remarks>Read rather than cached: "secret" resolves around round start and admins can change it mid-round.</remarks>
    private void RefreshGamemode()
    {
        var preset = _gameTicker.CurrentPreset;
        _recorder.SetGamemode(preset?.ID, preset is null ? null : Loc.GetString(preset.ModeTitle));
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        StopRecording(ev.RoundDuration);
    }

    /// <summary>Backstop for rounds restarted without ending. Runs before the entities are flushed.</summary>
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        // Already stopped on the normal path, where RoundEndedEvent came first.
        if (_recorder.IsRecording)
            StopRecording(null);
    }

    private void StopRecording(TimeSpan? duration)
    {
        // One final sample so the bundle ends on a complete picture. Not amortised: there is no next tick.
        if (_recorder.IsRecording)
        {
            // Re-read the preset: "secret" has resolved by now, and an admin may have changed it mid-round.
            RefreshGamemode();

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

    /// <summary>Joins the roster on first player control, and stays there for the rest of the round.</summary>
    /// <remarks>Every attachment also writes a control row: the roster records who an entity was, the control
    /// stream who was driving it, and those diverge the moment a body is cloned, borged or possessed.</remarks>
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

    /// <summary>Closes the control row opened by <see cref="OnPlayerAttached"/>.</summary>
    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        // Not gated on roster membership: gating would only ever drop the closing half of a pair.
        if (!_recorder.IsRecording || IsUninterestingObserver(ev.Entity))
            return;

        _recorder.WriteControl(ev.Entity, ev.Player.UserId.UserId, ev.Player.Name, attached: false);
    }

    /// <summary>Whether an entity is a pure observer whose movement carries no investigative meaning.</summary>
    /// <remarks>Keyed on <see cref="GhostComponent"/> specifically: a revenant is incorporeal but acts on the
    /// station and carries no such component, so it is kept.</remarks>
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
            _positionAccumulator -= _positionInterval;
            SamplePositions();
        }

        _navMapAccumulator += frameTime;
        if (_navMapAccumulator >= _navMapInterval)
        {
            _navMapAccumulator -= _navMapInterval;
            SampleNavMap();
        }

        _characterAccumulator += frameTime;
        if (_characterAccumulator >= _characterInterval)
        {
            _characterAccumulator -= _characterInterval;
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
                _dirtyAccumulator -= _dirtyInterval;
                DrainDirtyCharacters();
            }
        }

        _recorder.Update(frameTime);
    }

    #region Positions

    /// <inheritdoc/>
    /// <remarks>Used for speech from entities that are not on the roster, which have no sampled position.</remarks>
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
            // Inverting a grid matrix is not free and nearly everyone shares one grid. Cached for the sweep,
            // which is exactly as long as it stays valid: grids move between ticks, never within one.
        _gridMatrices.Clear();

        foreach (var uid in _recorder.Roster.Keys)
        {
            if (!_xformQuery.TryComp(uid, out var xform))
            {
                // Entity is gone. The roster entry stays as the record of who it was; only live caches are dropped.
                _recorder.UntrackEntity(uid);
                continue;
            }

                // Through containers: an entity in a locker reports the container's position, which is what we want.
            EntityUid? container = null;
            if (_container.TryGetContainingContainer((uid, xform, null), out var containing))
                container = containing.Owner;

            var grid = xform.GridUid;
            var worldPos = _transform.GetWorldPosition(uid);

                // Grid-local so a body on a moving shuttle does not smear. Off-grid falls back to world coordinates.
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

    /// <summary>Records total damage, mob state and the crit/dead thresholds for one entity.</summary>
    /// <remarks>Sampled here rather than in the slow character loop: a ten second snapshot would miss a whole fight.</remarks>
    private void SampleHealth(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        // GetTotalDamage is obsolete because content should not generally reduce damage to one number.
        // An investigation health bar is exactly that reduction, so it is the intended exception.
#pragma warning disable CS0618
        var total = (float) _damageable.GetTotalDamage((uid, damageable));
#pragma warning restore CS0618

        // Copied to a local: MobStateComponent is [Access]-restricted, and a direct call reads as execute access.
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

            // Beacons are the room labels a reader needs to answer "which room". Re-emitted only when they change.
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

    /// <summary>Fingerprint of a grid's whole beacon set.</summary>
    /// <remarks>Not the beacon count, which missed renames and recolours. Combined with XOR because dictionary
    /// enumeration order is not stable across mutations.</remarks>
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

    /// <summary>Queues every tracked character for re-snapshotting; the backstop for anything no event covers.</summary>
    /// <remarks>Queued rather than inline: a snapshot is expensive, and the whole roster at once is a spike
    /// proportional to player count landing every ten seconds.</remarks>
    private void BeginCharacterSweep()
    {
        // The previous sweep has not finished. Refilling would clear the tail it never reached and restart from
        // the top, so the same entities would be sampled forever and the ones after them never.
        if (_sweepQueue.Count > 0)
            return;

        foreach (var uid in _recorder.Roster.Keys)
        {
            _sweepQueue.Enqueue(uid);
        }

        // Everything on the roster is about to be sampled, so nothing is outstanding.
        _dirtyCharacters.Clear();
    }

    /// <summary>Snapshots the next slice of the queued backstop sweep.</summary>
    /// <remarks>Sized to cover the whole roster well inside one character interval; the point is to spread the work.</remarks>
    private void AdvanceCharacterSweep()
    {
        for (var sampled = 0; sampled < SweepBatchSize && _sweepQueue.TryDequeue(out var uid); sampled++)
        {
            // Entities die between being queued and reached; the roster keeps the entry, but there is nothing to snapshot.
            if (_metaQuery.HasComp(uid))
                SampleCharacter(uid);
        }
    }

    /// <summary>Re-snapshots only the characters whose equipment changed since the last drain.</summary>
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
        // Resolved once and shared: TryGetMind is a container walk rather than a component lookup.
        EntityUid? mindId = _mind.TryGetMind(uid, out var mind, out _) ? mind : null;

        var snapshot = BuildCharacterSnapshot(uid, mindId, out var fingerprint);
        _recorder.WriteCharacterIfChanged(uid, snapshot, fingerprint);

        if (mindId is { } ownedMind)
            SampleObjectives(uid, ownedMind);
    }

    /// <summary>Records progress on every objective this mind holds.</summary>
    /// <remarks>Polled rather than hooked, because there is no completion event: objectives compute progress on
    /// demand. Completion ticks therefore carry the sweep's granularity.</remarks>
    private void SampleObjectives(EntityUid owner, EntityUid mindId)
    {
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.Objectives.Count == 0)
            return;

        foreach (var objective in mind.Objectives)
        {
            // Null when an objective is malformed. Already logged by the objectives system; skipping is all we can do.
            if (_objectives.GetInfo(objective, mindId, mind) is not { } info)
                continue;

            var prototype = _metaQuery.TryComp(objective, out var meta) ? meta.EntityPrototype?.ID : null;

            _recorder.WriteObjectiveIfChanged(
                objective,
                owner,
                prototype,
                info.Title,
                info.Description,
                info.Progress);
        }
    }

    /// <summary>Marks a tracked entity for re-snapshotting on the next drain.</summary>
    /// <remarks>Does no work of its own: this runs inside equipment events, on the hot path of picking anything up.</remarks>
    private void MarkCharacterDirty(EntityUid uid)
    {
        if (_recorder.IsRecording && _recorder.Roster.ContainsKey(uid))
            _dirtyCharacters.Add(uid);
    }

    private object BuildCharacterSnapshot(EntityUid uid, EntityUid? mindId, out int fingerprint)
    {
        string? species = null;
        string? gender = null;
        var age = 0;

        if (TryComp<HumanoidProfileComponent>(uid, out var profile))
        {
            // Copied to locals: the component is [Access]-restricted, and direct calls read as execute access.
            var speciesProto = profile.Species;
            var profileGender = profile.Gender;

            species = speciesProto.Id;
            gender = profileGender.ToString();
            age = profile.Age;
        }

        string? job = null;
        List<AntagRole>? antagRoles = null;

        if (mindId is { } mind)
        {
            if (_jobs.MindTryGetJobId(mind, out var jobProto))
                job = jobProto?.Id;

            antagRoles = BuildAntagRoles(mind);
        }

        // Resolved here so the reader need not reimplement the job-to-department mapping or invent a palette.
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

        // Depth is capped so a bag of bags cannot blow up the snapshot.
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

            // Sorted here rather than in the fingerprint, which must be order-independent, so rows stay deterministic.
            carried.Sort(StringComparer.Ordinal);
        }

        var name = _metaQuery.TryComp(uid, out var meta) ? meta.EntityName : null;

        fingerprint = ComputeFingerprint(species, job, name, antagRoles, access, worn, held, carried);

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
            // Present only on antagonists, so the common row does not grow a field that is almost always empty.
            antag = antagRoles is { Count: > 0 } ? true : (bool?) null,
            roles = antagRoles is { Count: > 0 } ? antagRoles : null,
            access,
            worn,
            hands = held,
            carried,
        };
    }

    /// <summary>The antagonist roles on a mind, as prototype id plus localized name.</summary>
    /// <remarks>Recorded structurally so readers need not parse English out of <c>LogType.Mind</c> prose. Per
    /// snapshot rather than once, so a mid-round conversion appears at the tick it happened.</remarks>
    private List<AntagRole>? BuildAntagRoles(EntityUid mindId)
    {
        List<AntagRole>? antagRoles = null;

        foreach (var role in _roles.MindGetAllRoleInfo(mindId))
        {
            if (!role.Antagonist)
                continue;

            antagRoles ??= new List<AntagRole>();
            antagRoles.Add(new AntagRole(role.Prototype, Loc.GetString(role.Name)));
        }

        return antagRoles;
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

    /// <summary>One antagonist role on a character row.</summary>
    /// <param name="Id">Antag prototype id — the stable value a reader should branch on.</param>
    /// <param name="Name">Localized at record time, so the bundle reads without the game's locale files.</param>
    private readonly record struct AntagRole(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name);

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
        List<AntagRole>? antagRoles,
        List<string> access,
        Dictionary<string, string> worn,
        List<string> held,
        List<string> carried)
    {
        var hash = new HashCode();
        hash.Add(species);
        hash.Add(job);
        hash.Add(name);

        // Only prototype ids are hashed: the role container enumerates in a stable insertion order, and the
        // localized name is derived from the id.
        if (antagRoles != null)
        {
            foreach (var role in antagRoles)
            {
                hash.Add(role.Id);
            }
        }

        foreach (var tag in access)
            hash.Add(tag);

        // Slots come out in a fixed order and access and carried are sorted by their callers.
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
