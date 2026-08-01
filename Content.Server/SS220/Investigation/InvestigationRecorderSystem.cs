// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.Access.Systems;
using Content.Shared.SS220.CCVars;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Maps;
using Content.Shared.Inventory;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Objectives.Systems;
using Content.Server.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Investigation;

public sealed partial class InvestigationRecorderSystem : EntitySystem, IInvestigationPositionSource
{
    [Dependency] private readonly InvestigationRecorder _recorder = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
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

    private TimeSpan _positionInterval;
    private TimeSpan _navMapInterval;
    private TimeSpan _characterInterval;
    private TimeSpan _dirtyInterval;
    private float _positionEpsilon;
    private int _storageDepth;

    private TimeSpan _nextPositionSample;
    private TimeSpan _nextNavMapSample;
    private TimeSpan _nextCharacterSweep;
    private TimeSpan _nextDirtyDrain;
    private bool _wasRecording;

    private const int SweepBatchSize = 4;

    private readonly Queue<EntityUid> _sweepQueue = new();

    private readonly Dictionary<EntityUid, Matrix3x2> _gridMatrices = new();

    private Dictionary<string, string>? _departmentsByJob;

    private GameTick _lastNavMapTick = GameTick.Zero;

    private const float GridPoseEpsilon = 0.05f;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<InvestigationTrackedComponent> _trackedQuery;
    private EntityQuery<NavMapComponent> _navMapQuery;

    public override void Initialize()
    {
        base.Initialize();

        _recorder.Initialize();
        _recorder.SetPositionSource(this);

        _xformQuery = GetEntityQuery<TransformComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();
        _trackedQuery = GetEntityQuery<InvestigationTrackedComponent>();
        _navMapQuery = GetEntityQuery<NavMapComponent>();

        _cfg.OnValueChanged(CCVars220.InvestigationPositionInterval, i => _positionInterval = TimeSpan.FromSeconds(i), true);
        _cfg.OnValueChanged(CCVars220.InvestigationPositionEpsilon, epsilon => _positionEpsilon = epsilon, true);
        _cfg.OnValueChanged(CCVars220.InvestigationNavMapInterval, i => _navMapInterval = TimeSpan.FromSeconds(i), true);
        _cfg.OnValueChanged(CCVars220.InvestigationCharacterInterval, i => _characterInterval = TimeSpan.FromSeconds(i), true);
        _cfg.OnValueChanged(CCVars220.InvestigationDirtyInterval, i => _dirtyInterval = TimeSpan.FromSeconds(i), true);
        _cfg.OnValueChanged(CCVars220.InvestigationStorageDepth, depth => _storageDepth = depth, true);

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);

        // RoundEndedEvent misses restartround/golobby, which would otherwise never stop recording.
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<MindContainerComponent, DidEquipEvent>(OnEquipped);
        SubscribeLocalEvent<MindContainerComponent, DidUnequipEvent>(OnUnequipped);
        SubscribeLocalEvent<MindContainerComponent, DidEquipHandEvent>(OnHandEquipped);
        SubscribeLocalEvent<MindContainerComponent, DidUnequipHandEvent>(OnHandUnequipped);

        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);

        _prototypes.PrototypesReloaded += _ => _departmentsByJob = null;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _recorder.Shutdown();
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        _lastNavMapTick = GameTick.Zero;
        ResetGridState();
        _sweepQueue.Clear();
        _gridMatrices.Clear();
        ResetSampleTimers(_timing.CurTime);

        RefreshGamemode();

        _recorder.StartRound(ev.RoundId, _gameMap.GetSelectedMap()?.MapName);
    }

    private void RefreshGamemode()
    {
        var preset = _gameTicker.CurrentPreset;
        _recorder.SetGamemode(preset?.ID, preset is null ? null : Loc.GetString(preset.ModeTitle));
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        StopRecording(ev.RoundDuration);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        if (_recorder.IsRecording)
            StopRecording(null);
    }

    private void StopRecording(TimeSpan? duration)
    {
        if (_recorder.IsRecording)
        {
            RefreshGamemode();

            SamplePositions();
            SampleGridPoses();
            SampleNavMap();
            SampleGridFootprints();

            var query = EntityQueryEnumerator<InvestigationTrackedComponent>();
            while (query.MoveNext(out var uid, out var tracked))
            {
                SampleCharacter((uid, tracked));
            }

        }

        _recorder.StopRound(duration);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!_recorder.IsRecording || IsUninterestingObserver(ev.Entity))
            return;

        if (!_trackedQuery.HasComp(ev.Entity))
        {
            var name = _metaQuery.TryComp(ev.Entity, out var meta) ? meta.EntityName : "<unknown>";
            var prototype = meta?.EntityPrototype?.ID;

            _recorder.TrackEntity(ev.Entity, ev.Player.UserId.UserId, ev.Player.Name, name, prototype);
            AddComp<InvestigationTrackedComponent>(ev.Entity);
        }

        _recorder.WriteControl(ev.Entity, ev.Player.UserId.UserId, ev.Player.Name, attached: true);
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        if (!_recorder.IsRecording || IsUninterestingObserver(ev.Entity))
            return;

        _recorder.WriteControl(ev.Entity, ev.Player.UserId.UserId, ev.Player.Name, attached: false);
    }

    /// <summary>Keyed on <see cref="GhostComponent"/>: a revenant is incorporeal but acts, so it is kept.</summary>
    private void ResetGridState()
    {
        var query = EntityQueryEnumerator<InvestigationGridComponent>();
        while (query.MoveNext(out _, out var grid))
        {
            grid.Pose = null;
            grid.BeaconHash = null;
            grid.SentFullSnapshot = false;
        }
    }

    private void ResetSampleTimers(TimeSpan now)
    {
        _nextPositionSample = now + _positionInterval;
        _nextNavMapSample = now + _navMapInterval;
        _nextCharacterSweep = now + _characterInterval;
        _nextDirtyDrain = now + _dirtyInterval;
    }

    private static bool Elapsed(ref TimeSpan next, TimeSpan interval, TimeSpan now)
    {
        if (now < next)
            return false;

        next += interval;
        if (next <= now)
            next = now + interval;

        return true;
    }

    private void OnEquipped(Entity<MindContainerComponent> ent, ref DidEquipEvent args) => MarkCharacterDirty(ent);

    private void OnUnequipped(Entity<MindContainerComponent> ent, ref DidUnequipEvent args) => MarkCharacterDirty(ent);

    private void OnHandEquipped(Entity<MindContainerComponent> ent, ref DidEquipHandEvent args) => MarkCharacterDirty(ent);

    private void OnHandUnequipped(Entity<MindContainerComponent> ent, ref DidUnequipHandEvent args) => MarkCharacterDirty(ent);

    private void OnMindAdded(Entity<MindContainerComponent> ent, ref MindAddedMessage args) => MarkCharacterDirty(ent);

    private void OnMindRemoved(Entity<MindContainerComponent> ent, ref MindRemovedMessage args) => MarkCharacterDirty(ent);

    private bool IsUninterestingObserver(EntityUid uid)
    {
        return _ghostQuery.HasComp(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_recorder.IsRecording)
        {
            _wasRecording = false;
            return;
        }

        var now = _timing.CurTime;

        if (!_wasRecording)
        {
            _wasRecording = true;
            ResetSampleTimers(now);
        }

        if (Elapsed(ref _nextPositionSample, _positionInterval, now))
            SamplePositions();

        if (Elapsed(ref _nextNavMapSample, _navMapInterval, now))
        {
            SampleGridPoses();
            SampleNavMap();
            SampleGridFootprints();
        }

        if (Elapsed(ref _nextCharacterSweep, _characterInterval, now))
            BeginCharacterSweep();

        if (_sweepQueue.Count > 0)
            AdvanceCharacterSweep();
        else if (Elapsed(ref _nextDirtyDrain, _dirtyInterval, now))
            DrainDirtyCharacters();

        _recorder.Update();
    }
}
