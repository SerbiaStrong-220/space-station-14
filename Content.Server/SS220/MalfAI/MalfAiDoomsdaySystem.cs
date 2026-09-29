// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Electrocution;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.StationEvents.Components;

using Content.Server.StationEvents.Events;
using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Metabolism;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Station.Components;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.MalfAI;

public sealed partial class MalfAiDoomsdaySystem : StationEventSystem<MalfAiDoomsdayComponent>
{
    [Dependency] private AlertLevelSystem _alerts = default!;
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private EmergencyShuttleSystem _emergency = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private MalfAiSystem _malfAi = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private PvsOverrideSystem _pvs = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly EntProtoId DoomsdayRuleProto = "MalfAiDoomsdayRule";
    private static readonly EntProtoId DoomsdayWaveProto = "MalfAiDoomsdayWave";
    private static readonly EntProtoId EmpPulseProto = "EffectEmpPulse";
    private static readonly EntProtoId SparksProto = "EffectSparks";
    private static readonly ProtoId<DamageTypePrototype> DamageTypeShock = "Shock";

    private const string DeltaLevel = "delta";
    private static readonly LocId ArmAnnouncement = "malfai-doomsday-start-announcement";
    private static readonly LocId CancelAnnouncement = "malfai-doomsday-cancel-announcement";
    private static readonly LocId WaveAnnouncement = "malfai-doomsday-wave-announcement";
    private static readonly LocId CompleteAnnouncement = "malfai-doomsday-complete-announcement";

    private static readonly LocId DoomsdayActive = "malfai-doomsday-active";
    private static readonly LocId DoomsdayShuttleDeparting = "malfai-doomsday-shuttle-departing";
    private static readonly LocId ModuleNotOperational = "malfai-module-not-operational";
    private static readonly LocId ModuleRefunded = "malfai-module-refunded";
    private static readonly LocId DoomsdayArmed = "malfai-doomsday-armed";
    private static readonly LocId DoomsdayShuttleBlocked = "malfai-doomsday-shuttle-blocked";

    private const string ReasonDeleted = "deleted";
    private const string ReasonNoBody = "no-body";
    private const string ReasonNotOperational = "not-operational";
    private const string ReasonNoPower = "no-power";

    private const float MinRadius = 30f;
    private const float RadiusMargin = 15f;

    private static readonly SoundSpecifier AnnounceSound =
        new SoundPathSpecifier("/Audio/Announcements/attention.ogg")
        {
            Params = AudioParams.Default.WithVolume(-4f),
        };

    private readonly List<(EntityUid Uid, MalfAiDoomsdayWaveComponent Wave, MalfAiDoomsdayComponent Doom)> _pulseActive = new();
    private readonly List<EntityUid> _pulseHit = new();
    private readonly Dictionary<EntityUid, HashSet<ICommonSession>> _wavePvsSessions = new();
    private readonly HashSet<ICommonSession> _wavePvsTarget = new();

    [Dependency] private EntityQuery<SiliconLawBoundComponent> _siliconQuery = default!;
    [Dependency] private EntityQuery<BorgChassisComponent> _borgQuery = default!;
    [Dependency] private EntityQuery<StationAiHeldComponent> _aiHeldQuery = default!;
    [Dependency] private EntityQuery<StationAiCoreComponent> _aiCoreQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(MetabolizerSystem));
        SubscribeLocalEvent<MalfModuleExecuteEvent>(OnDoomsdayModule);
        SubscribeLocalEvent<MalfAiDoomsdayComponent, ComponentShutdown>(OnRuleShutdown);
        SubscribeLocalEvent<MalfAiDoomsdayCoreComponent, EntityTerminatingEvent>(OnCoreTerminating);
        SubscribeLocalEvent<MalfAiDoomsdayCoreComponent, PowerChangedEvent>(OnCorePowerChanged);
        SubscribeLocalEvent<MalfAiDoomsdayCoreComponent, EntRemovedFromContainerMessage>(OnBrainRemoved);
        SubscribeLocalEvent<MalfAiDoomsdayWaveComponent, EntityTerminatingEvent>(OnWaveTerminating);
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
        SubscribeLocalEvent<EmergencyShuttleEarlyLaunchAttemptEvent>(OnEarlyLaunchAttempt);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleLost);
    }


    private void OnRoleLost(RoleRemovedEvent args)
    {
        if (_malfAi.HasLiveMalfRole(args.MindId))
            return;

        var query = EntityQueryEnumerator<MalfAiDoomsdayComponent>();
        while (query.MoveNext(out var uid, out var doom))
        {
            if (doom.MindId != args.MindId)
                continue;

            TryCancel(uid, "role-lost");
            return;
        }
    }

    private void OnDoomsdayModule(ref MalfModuleExecuteEvent args)
    {
        if (args.ListingId != MalfAiConstants.DoomsdayListing)
            return;

        args.Handled = true;
        var ctx = args.Context;

        if (HasActiveDoomsday(ctx.Station))
        {
            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.DoomsdayListing);
            _popup.PopupEntity(Loc.GetString(DoomsdayActive), ctx.Performer, ctx.Performer);
            return;
        }

        if (_emergency.EarlyLaunchAuthorized || _emergency.ShuttlesLeft)
        {

            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.DoomsdayListing);
            _popup.PopupEntity(Loc.GetString(DoomsdayShuttleDeparting), ctx.Performer, ctx.Performer);
            return;
        }

        if (!_stationAi.TryGetCore(ctx.Performer, out var core) || core.Comp == null)
        {
            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.DoomsdayListing);
            _popup.PopupEntity(Loc.GetString(ModuleNotOperational), ctx.Performer, ctx.Performer);
            return;
        }

        var rule = _ticker.AddGameRule(DoomsdayRuleProto);
        var doom = EnsureComp<MalfAiDoomsdayComponent>(rule);
        doom.Station = ctx.Station;
        doom.Core = core.Owner;
        doom.MindId = ctx.MindId;
        doom.Role = ctx.Role.Owner;
        doom.Phase = MalfAiDoomsdayPhase.Armed;

        if (TryComp<StationEventComponent>(rule, out var stationEvent))
            stationEvent.Duration = TimeSpan.FromSeconds(Math.Max(0.1f, _cfg.GetCVar(CCVars220.MalfAiDoomsdayDuration)));

        if (!_ticker.StartGameRule(rule))
        {
            doom.Phase = MalfAiDoomsdayPhase.Cancelled;
            QueueDel(rule);

            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.DoomsdayListing);
            _popup.PopupEntity(Loc.GetString(ModuleRefunded), ctx.Performer, ctx.Performer);
            return;
        }

        var seconds = (int) (stationEvent?.Duration?.TotalSeconds ?? _cfg.GetCVar(CCVars220.MalfAiDoomsdayDuration));
        _popup.PopupEntity(Loc.GetString(DoomsdayArmed, ("seconds", seconds)),
            ctx.Performer, ctx.Performer);
        _admin.Add(LogType.Action, LogImpact.Extreme,
            $"{ToPrettyString(ctx.Performer):player} (Malf AI) armed Doomsday " +
            $"on station {ctx.Station} (core {ToPrettyString(core.Owner):target}, {seconds}s).");
    }

    protected override void Started(EntityUid uid, MalfAiDoomsdayComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (TerminatingOrDeleted(component.Station) || TerminatingOrDeleted(component.Core))
        {
            _ticker.EndGameRule(uid, gameRule);
            return;
        }

        EnsureComp<MalfAiDoomsdayCoreComponent>(component.Core).Rule = uid;
        ArmAlert(component.Station, component);
        RecallShuttle();
        AnnounceToStation(component.Station, ArmAnnouncement, playSound: true);
    }

    protected override void Ended(EntityUid uid, MalfAiDoomsdayComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        CleanupCore(component);

        if (component.Phase != MalfAiDoomsdayPhase.Armed)
            return;

        if (!StillArmed(component, out var reason))
        {
            Cancel(uid, component, reason, endRule: false);
            return;
        }

        component.Phase = MalfAiDoomsdayPhase.Wave;
        StartWave(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Count<MalfAiDoomsdayComponent>() == 0)
            return;

        _pulseActive.Clear();
        var query = EntityQueryEnumerator<MalfAiDoomsdayComponent>();
        while (query.MoveNext(out var uid, out var doom))
        {
            if (!WaveRunning(doom))
                continue;

            if (TerminatingOrDeleted(doom.WaveEntity) || !TryComp<MalfAiDoomsdayWaveComponent>(doom.WaveEntity, out var wave))
            {
                wave = RestoreWaveVisual(doom);
                if (wave == null)
                {
                    FinishWave(uid, doom);
                    continue;
                }
            }

            UpdateWavePvs(doom.WaveEntity, doom.Station);
            _pulseActive.Add((uid, wave, doom));
        }

        foreach (var (uid, wave, doom) in _pulseActive)
            PulseWave(uid, doom, wave);
    }

    private void OnRuleShutdown(Entity<MalfAiDoomsdayComponent> ent, ref ComponentShutdown args)
    {
        CleanupCore(ent.Comp);

        if (ent.Comp.Phase == MalfAiDoomsdayPhase.Armed)
        {
            ent.Comp.Phase = MalfAiDoomsdayPhase.Cancelled;
            RevertAlert(ent.Comp);
            CleanupWave(ent.Comp);
            _malfAi.RefundListing(ent.Comp.MindId, MalfAiConstants.DoomsdayListing);
            if (!TerminatingOrDeleted(ent.Comp.Station))
                AnnounceToStation(ent.Comp.Station, CancelAnnouncement, playSound: false);
            return;
        }

        if (ent.Comp.Phase == MalfAiDoomsdayPhase.Wave)
            ent.Comp.Phase = MalfAiDoomsdayPhase.Completed;

        CleanupWave(ent.Comp);
    }

    private bool HasActiveDoomsday(EntityUid station)
    {
        var query = EntityQueryEnumerator<MalfAiDoomsdayComponent>();
        while (query.MoveNext(out _, out var doom))
        {
            if (doom.Station == station && doom.Phase is MalfAiDoomsdayPhase.Armed or MalfAiDoomsdayPhase.Wave)
                return true;
        }

        return false;
    }

    private void ArmAlert(EntityUid station, MalfAiDoomsdayComponent doom)
    {
        doom.PreviousAlertLevel = _alerts.GetLevel(station);
        doom.WeSetAlert = false;

        if (doom.PreviousAlertLevel == DeltaLevel)
            return;

        if (!TryComp<AlertLevelComponent>(station, out var alert) || alert.IsLevelLocked)
            return;

        _alerts.SetLevel(station, DeltaLevel, true, true, force: true, locked: true);
        if (_alerts.GetLevel(station) == DeltaLevel)
            doom.WeSetAlert = true;
    }

    private void RecallShuttle()
    {
        if (_emergency.EmergencyShuttleArrived)
            return;

        if (_roundEnd.IsRoundEndRequested())
            _roundEnd.CancelRoundEndCountdown(forceRecall: true);
    }

    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<MalfAiDoomsdayComponent>();
        while (query.MoveNext(out _, out var doom))
        {
            if (!IsLaunchBlocking(doom))
                continue;

            ev.Cancelled = true;
            ev.Reason = Loc.GetString(DoomsdayShuttleBlocked);
            return;
        }
    }

    private void OnEarlyLaunchAttempt(ref EmergencyShuttleEarlyLaunchAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<MalfAiDoomsdayComponent>();
        while (query.MoveNext(out _, out var doom))
        {
            if (!IsLaunchBlocking(doom))
                continue;

            ev.Cancelled = true;
            return;
        }
    }

    private static bool IsLaunchBlocking(MalfAiDoomsdayComponent doom)
        => doom.Phase is MalfAiDoomsdayPhase.Armed or MalfAiDoomsdayPhase.Wave;


    private void OnCoreTerminating(Entity<MalfAiDoomsdayCoreComponent> ent, ref EntityTerminatingEvent args)
    {
        TryCancel(ent.Comp.Rule, "core-destroyed");
    }

    private void OnCorePowerChanged(Entity<MalfAiDoomsdayCoreComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered || !CoreNeedsPower(ent.Owner))
            return;

        TryCancel(ent.Comp.Rule, ReasonNoPower);
    }

    private void OnBrainRemoved(Entity<MalfAiDoomsdayCoreComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != StationAiCoreComponent.Container)
            return;

        TryCancel(ent.Comp.Rule, ReasonNotOperational);
    }

    private void TryCancel(EntityUid rule, string reason)
    {
        if (!TryComp<MalfAiDoomsdayComponent>(rule, out var doom)
            || doom.Phase != MalfAiDoomsdayPhase.Armed)
            return;

        Cancel(rule, doom, reason);
    }

    private bool StillArmed(MalfAiDoomsdayComponent doom, out string reason)
    {
        reason = "cancelled";

        if (TerminatingOrDeleted(doom.Station) || TerminatingOrDeleted(doom.Core) || TerminatingOrDeleted(doom.Role))
        {
            reason = ReasonDeleted;
            return false;
        }

        if (!TryComp<MindComponent>(doom.MindId, out var mind) || mind.OwnedEntity is not { } body)
        {
            reason = ReasonNoBody;
            return false;
        }

        if (!_malfAi.IsMalfOperational(body))
        {
            reason = ReasonNotOperational;
            return false;
        }

        if (CoreNeedsPower(doom.Core) && !this.IsPowered(doom.Core, EntityManager))
        {
            reason = ReasonNoPower;
            return false;
        }

        return true;
    }

    private void Cancel(EntityUid rule, MalfAiDoomsdayComponent doom, string reason, bool endRule = true)
    {
        if (doom.Phase != MalfAiDoomsdayPhase.Armed)
            return;

        doom.Phase = MalfAiDoomsdayPhase.Cancelled;
        RevertAlert(doom);
        CleanupCore(doom);

        if (!TerminatingOrDeleted(doom.Station))
            AnnounceToStation(doom.Station, CancelAnnouncement, playSound: false);

        _malfAi.RefundListing(doom.MindId, MalfAiConstants.DoomsdayListing);
        _admin.Add(LogType.Action, LogImpact.High,
            $"Malf AI Doomsday cancelled ({reason}) on station {doom.Station}.");

        if (endRule && !TerminatingOrDeleted(rule))
            _ticker.EndGameRule(rule);
    }

    private void CleanupCore(MalfAiDoomsdayComponent doom)
    {
        if (!TerminatingOrDeleted(doom.Core))
            RemCompDeferred<MalfAiDoomsdayCoreComponent>(doom.Core);
    }

    private void RevertAlert(MalfAiDoomsdayComponent doom)
    {
        if (TerminatingOrDeleted(doom.Station) || !doom.WeSetAlert)
            return;

        if (_alerts.GetLevel(doom.Station) != DeltaLevel)
            return;

        var restore = doom.PreviousAlertLevel;
        if (string.IsNullOrEmpty(restore) || restore == DeltaLevel)
            restore = _alerts.GetDefaultLevel(doom.Station);

        if (string.IsNullOrEmpty(restore) || restore == DeltaLevel)
            return;

        _alerts.SetLevel(doom.Station, restore, true, true, force: true, locked: false);
    }

    private void StartWave(EntityUid rule, MalfAiDoomsdayComponent doom)
    {
        var origin = _xform.GetWorldPosition(doom.Core);
        doom.WaveKilled = 0;
        doom.LastFxRadius = 0f;

        doom.StationRadius = 0f;
        doom.Hit.Clear();
        doom.RingRadius = [];

        var cvarMax = Math.Max(1f, _cfg.GetCVar(CCVars220.MalfAiDoomsdayWaveRadius));
        var maxRadius = cvarMax;
        if (TryComp<StationDataComponent>(doom.Station, out var stationData))
        {
            foreach (var grid in stationData.Grids)
            {
                var aabb = _lookup.GetWorldAABB(grid);
                doom.StationRadius = MathF.Max(doom.StationRadius,
                    (aabb.Center - origin).Length() + aabb.Size.Length() * 0.5f);
            }

            if (doom.StationRadius > 0f)
                maxRadius = Math.Min(cvarMax, Math.Max(MinRadius, doom.StationRadius + RadiusMargin));
        }

        var visual = Spawn(DoomsdayWaveProto, Transform(doom.Core).Coordinates);
        doom.WaveEntity = visual;
        doom.WaveStartTime = Timing.CurTime;
        doom.WaveSpeed = Math.Max(0.1f, _cfg.GetCVar(CCVars220.MalfAiDoomsdayWaveSpeed));
        doom.WaveMaxRadius = maxRadius;
        doom.WaveMapId = Transform(doom.Core).MapID;
        doom.WaveOrigin = origin;

        var wave = EnsureComp<MalfAiDoomsdayWaveComponent>(visual);
        CopyWaveState(doom, wave);
        Dirty(visual, wave);
        doom.RingRadius = new float[wave.RingCount];
        UpdateWavePvs(visual, doom.Station);

        if (!TerminatingOrDeleted(doom.Station) && !doom.WaveAnnounced)
        {
            doom.WaveAnnounced = true;
            AnnounceToStation(doom.Station, WaveAnnouncement, playSound: true);
        }

        _admin.Add(LogType.Action, LogImpact.Extreme,
            $"Malf AI Doomsday pulse started on station {doom.Station} from core {ToPrettyString(doom.Core):target}.");
    }

    private void PulseWave(EntityUid rule, MalfAiDoomsdayComponent doom, MalfAiDoomsdayWaveComponent wave)
    {
        var elapsed = (float) (Timing.CurTime - doom.WaveStartTime).TotalSeconds;
        var rings = Math.Max(1, wave.RingCount);
        if (doom.RingRadius.Length != rings)
            doom.RingRadius = new float[rings];

        var lead = 0f;
        for (var i = 0; i < rings; i++)
        {
            var local = elapsed - i * wave.RingDelay;
            doom.RingRadius[i] = local <= 0f ? 0f : Math.Min(doom.WaveMaxRadius, local * doom.WaveSpeed);
            lead = Math.Max(lead, doom.RingRadius[i]);
        }

        SpawnRimFx(doom, wave, lead);

        _pulseHit.Clear();
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mob, out var xform))
        {
            if (doom.Hit.Contains(uid))
                continue;

            if (xform.MapID != doom.WaveMapId)
                continue;

            if (!_mobState.IsAlive(uid, mob) && !_mobState.IsCritical(uid, mob))
                continue;

            var distanceSquared = (_xform.GetWorldPosition(uid) - doom.WaveOrigin).LengthSquared();
            if (lead * lead < distanceSquared)
                continue;

            if (_siliconQuery.HasComp(uid)
                || _borgQuery.HasComp(uid)
                || _aiHeldQuery.HasComp(uid)
                || _aiCoreQuery.HasComp(uid))
                continue;

            _pulseHit.Add(uid);
        }

        var source = TerminatingOrDeleted(doom.Core) ? (EntityUid?) null : doom.Core;
        foreach (var uid in _pulseHit)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            doom.Hit.Add(uid);
            _electrocution.TryDoElectrocution(
                uid,
                source,
                (int)wave.ElectrocuteDamage,
                TimeSpan.FromSeconds(wave.ElectrocuteSeconds),
                refresh: true,
                ignoreInsulation: true);

            var damage = new DamageSpecifier(_proto.Index(DamageTypeShock), wave.ShockDamage);
            _damageable.TryChangeDamage(uid, damage, ignoreResistances: true, origin: source);
            doom.WaveKilled++;
        }

        if (doom.RingRadius[^1] < doom.WaveMaxRadius)
            return;

        FinishWave(rule, doom);
    }

    private void SpawnRimFx(MalfAiDoomsdayComponent doom, MalfAiDoomsdayWaveComponent wave, float radius)
    {
        if (radius - doom.LastFxRadius < wave.FxStep)
            return;

        if (doom.StationRadius > 0f && radius > doom.StationRadius + RadiusMargin)
            return;

        doom.LastFxRadius = radius;
        var count = Math.Max(1, wave.FxCount);
        var phase = radius * 0.37f;
        for (var i = 0; i < count; i++)
        {
            var ang = phase + i * (MathF.Tau / count);
            var pos = doom.WaveOrigin + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * radius;
            var coords = _xform.ToCoordinates(new MapCoordinates(pos, doom.WaveMapId));
            Spawn(i % 2 == 0 ? EmpPulseProto : SparksProto, coords);
        }
    }

    private void FinishWave(EntityUid rule, MalfAiDoomsdayComponent doom)
    {
        if (doom.Phase != MalfAiDoomsdayPhase.Wave)
            return;

        doom.Phase = MalfAiDoomsdayPhase.Completed;
        CleanupWave(doom);
        if (!TerminatingOrDeleted(doom.Station))
            AnnounceToStation(doom.Station, CompleteAnnouncement, playSound: true);
        _admin.Add(LogType.Action, LogImpact.Extreme,
            $"Malf AI Doomsday wave finished on station {doom.Station} ({doom.WaveKilled} organics killed).");

        if (_ticker.RunLevel == GameRunLevel.InRound)
            _roundEnd.EndRound();
    }

    private static bool WaveRunning(MalfAiDoomsdayComponent doom)
        => doom.Phase == MalfAiDoomsdayPhase.Wave;

    private MalfAiDoomsdayWaveComponent? RestoreWaveVisual(MalfAiDoomsdayComponent doom)
    {
        if (doom.Phase != MalfAiDoomsdayPhase.Wave)
            return null;

        if (!_map.TryGetMap(doom.WaveMapId, out _))
            return null;

        var visual = Spawn(DoomsdayWaveProto, _xform.ToCoordinates(new MapCoordinates(doom.WaveOrigin, doom.WaveMapId)));

        doom.WaveEntity = visual;
        var wave = EnsureComp<MalfAiDoomsdayWaveComponent>(visual);
        CopyWaveState(doom, wave);
        Dirty(visual, wave);
        UpdateWavePvs(visual, doom.Station);
        return wave;
    }

    private static void CopyWaveState(MalfAiDoomsdayComponent doom, MalfAiDoomsdayWaveComponent wave)
    {
        wave.Origin = doom.WaveOrigin;
        wave.MapId = doom.WaveMapId;
        wave.StartTime = doom.WaveStartTime;
        wave.Speed = doom.WaveSpeed;
        wave.MaxRadius = doom.WaveMaxRadius;
    }

    private void UpdateWavePvs(EntityUid visual, EntityUid station)
    {
        if (!_wavePvsSessions.TryGetValue(visual, out var sessions))
        {
            sessions = new HashSet<ICommonSession>();
            _wavePvsSessions[visual] = sessions;
        }

        if (!TryComp<StationDataComponent>(station, out var stationData))
        {
            RemoveWavePvs(visual);
            return;
        }

        var target = _wavePvsTarget;
        target.Clear();
        foreach (var session in StationSystem.GetInStation(stationData).Recipients)
            target.Add(session);

        sessions.RemoveWhere(session =>
        {
            if (target.Contains(session))
                return false;

            _pvs.RemoveSessionOverride(visual, session);
            return true;
        });

        foreach (var session in target)
        {
            if (sessions.Add(session))
                _pvs.AddSessionOverride(visual, session);
        }
    }

    private void RemoveWavePvs(EntityUid visual)
    {
        if (!_wavePvsSessions.Remove(visual, out var sessions))
            return;

        foreach (var session in sessions)
            _pvs.RemoveSessionOverride(visual, session);
    }

    private bool CoreNeedsPower(EntityUid core)
    {
        return !TryComp<ApcPowerReceiverComponent>(core, out var recv) || recv.NeedsPower;
    }

    private void OnWaveTerminating(Entity<MalfAiDoomsdayWaveComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveWavePvs(ent.Owner);

        var query = EntityQueryEnumerator<MalfAiDoomsdayComponent>();
        while (query.MoveNext(out _, out var doom))
        {
            if (doom.WaveEntity != ent.Owner)
                continue;

            doom.WaveEntity = default;
            return;
        }
    }

    private void CleanupWave(MalfAiDoomsdayComponent doom)
    {
        var visual = doom.WaveEntity;
        doom.WaveEntity = default;
        RemoveWavePvs(visual);
        if (visual != default && !TerminatingOrDeleted(visual))
            QueueDel(visual);
    }

    private void AnnounceToStation(EntityUid station, LocId textKey, bool playSound)
    {
        if (!TryComp<StationDataComponent>(station, out var stationData))
            return;

        var filter = StationSystem.GetInStation(stationData);
        ChatSystem.DispatchFilteredAnnouncement(filter, Loc.GetString(textKey), playSound: playSound,
            announcementSound: AnnounceSound, colorOverride: Color.Red, playTTS: false);
    }
}
