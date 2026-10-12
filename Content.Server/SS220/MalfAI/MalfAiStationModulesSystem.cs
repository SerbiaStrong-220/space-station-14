// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Server.SurveillanceCamera;
using Content.Shared.Database;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Emp;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.SS220.IgnoreLightVision.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.MalfAI;

public sealed partial class MalfAiStationModulesSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private AirAlarmSystem _airAlarm = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MalfAiSystem _malfAi = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedEmpSystem _emp = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private SurveillanceCameraSystem _cameras = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly SoundSpecifier BlackoutSparkSound = new SoundPathSpecifier("/Audio/Effects/sparks4.ogg");
    private static readonly EntProtoId SparksProto = "EffectSparks";

    private static readonly LocId AbilityProtected = "malfai-ability-protected";
    private static readonly LocId HackNoPower = "malfai-hack-no-power";
    private static readonly LocId AbilityNoVision = "malfai-ability-no-vision";
    private static readonly LocId ThermalAlready = "malfai-thermal-already";
    private static readonly LocId ThermalDone = "malfai-thermal-done";
    private static readonly LocId SilentRecordsDone = "malfai-silent-records-done";
    private static readonly LocId CameraAlready = "malfai-camera-already";
    private static readonly LocId CameraDone = "malfai-camera-done";
    private static readonly LocId FloodDone = "malfai-flood-done";
    private static readonly LocId ChaosFailed = "malfai-chaos-failed";
    private static readonly LocId ChaosDone = "malfai-chaos-done";

    private const float BlackoutIgniteTemp = 700f;
    private const float BlackoutIgniteVolume = 50f;
    private const float BlackoutEmpRange = 6f;
    private const float BlackoutEmpEnergy = 25000f;
    private const float BlackoutEmpDuration = 15f;

    private readonly record struct BlackoutTuning(float Temp, float Volume, float EmpRange, float EmpEnergy, TimeSpan EmpDuration);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MalfModuleExecuteEvent>(OnModuleExecute);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiBlackoutEvent>(OnBlackout);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiAirFloodEvent>(OnAirFlood);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiChaosPulseEvent>(OnChaosPulse);
        SubscribeLocalEvent<StationAiVisionComponent, ComponentStartup>(OnCameraSpawned);
    }

    private void OnModuleExecute(ref MalfModuleExecuteEvent args)
    {
        if (args.ListingId == MalfAiConstants.ThermalListing)
        {
            args.Handled = true;
            DoThermalOverride(args.Context);
        }
        else if (args.ListingId == MalfAiConstants.SilentRecordsListing)
        {
            args.Handled = true;
            DoSilentRecords(args.Context);
        }
        else if (args.ListingId == MalfAiConstants.CameraUpgradeListing)
        {
            args.Handled = true;
            DoCameraUpgrade(args.Context);
        }
    }

    private void OnBlackout(Entity<MalfAiActorComponent> ent, ref MalfAiBlackoutEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        var target = args.Target;

        if (!_malfAi.TryGetMalfContext(performer, out var ctx))
            return;

        if (TerminatingOrDeleted(target) || !TryComp<ApcComponent>(target, out var apc))
            return;

        var targetXform = Transform(target);

        if (HasComp<HighPriorityAPCComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString(AbilityProtected), performer, performer);
            return;
        }

        if (!apc.MainBreakerEnabled || _battery.GetCharge(target) <= 0f)
        {
            _popup.PopupEntity(Loc.GetString(HackNoPower), performer, performer);
            return;
        }

        if (!_malfAi.IsVisibleToAi(target, targetXform))
        {
            _popup.PopupEntity(Loc.GetString(AbilityNoVision), performer, performer);
            return;
        }

        args.Handled = true;
        DoBlackout(ctx.Value, (target, targetXform), ResolveBlackoutTuning(args.Action));
    }

    private BlackoutTuning ResolveBlackoutTuning(EntityUid action)
    {
        if (TryComp<MalfAiBlackoutTuningComponent>(action, out var tune))
        {
            return new BlackoutTuning(tune.IgniteTemp,
                tune.IgniteVolume,
                tune.EmpRange,
                tune.EmpEnergy,
                TimeSpan.FromSeconds(tune.EmpDuration));
        }

        return new BlackoutTuning(BlackoutIgniteTemp,
            BlackoutIgniteVolume,
            BlackoutEmpRange,
            BlackoutEmpEnergy,
            TimeSpan.FromSeconds(BlackoutEmpDuration));
    }

    private void DoBlackout(MalfExecutionContext ctx, Entity<TransformComponent> apc, BlackoutTuning tuning)
    {
        var performer = ctx.Performer;

        var ignited = IgniteApcArea(apc, tuning);

        _emp.EmpPulse(apc.Comp.Coordinates, tuning.EmpRange, tuning.EmpEnergy, tuning.EmpDuration, performer);

        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) triggered a directed Blackout " +
            $"on {ToPrettyString(apc.Owner):target} ({ignited} tiles ignited).");
    }

    private int IgniteApcArea(Entity<TransformComponent> apc, BlackoutTuning tuning)
    {
        var xform = apc.Comp;
        Spawn(SparksProto, xform.Coordinates);
        _audio.PlayPvs(BlackoutSparkSound, xform.Coordinates);

        if (xform.GridUid is not { } gridUid)
            return 0;

        if (!TryComp<GridAtmosphereComponent>(gridUid, out var gridAtmos))
            return 0;

        if (xform.MapUid is not { } mapUid)
            return 0;

        TryComp<MapAtmosphereComponent>(mapUid, out var mapAtmos);

        Entity<GridAtmosphereComponent?> gridEnt = new(gridUid, gridAtmos);
        Entity<MapAtmosphereComponent?> mapEnt = new(mapUid, mapAtmos);
        var origin = _xform.GetGridOrMapTilePosition(apc, xform);
        var ignited = 0;
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (TryExposeTile(gridUid, origin + new Vector2i(dx, dy), apc, gridEnt, mapEnt, tuning)
                    == ExposeResult.Ignited)
                    ignited++;
            }
        }

        return ignited;
    }

    private enum ExposeResult
    {
        Skipped,
        Ignited,
        NoFuel,
        NoOxygen,
    }

    private ExposeResult TryExposeTile(EntityUid gridUid,
        Vector2i tile,
        EntityUid src,
        Entity<GridAtmosphereComponent?> gridEnt,
        Entity<MapAtmosphereComponent?> mapEnt,
        BlackoutTuning tuning)
    {
        if (_atmos.IsTileSpace(gridEnt, mapEnt, tile)
            || _atmos.IsTileAirBlocked(gridUid, tile))
            return ExposeResult.Skipped;

        _atmos.HotspotExpose(gridEnt, tile, tuning.Temp, tuning.Volume, src, true);
        if (_atmos.GetTileMixture(gridEnt, mapEnt, tile) is not { } mix)
            return ExposeResult.NoFuel;

        if (_atmos.IsMixtureIgnitable(mix))
            return ExposeResult.Ignited;

        return _atmos.IsMixtureFuel(mix) ? ExposeResult.NoOxygen : ExposeResult.NoFuel;
    }

    private void DoThermalOverride(MalfExecutionContext ctx)
    {
        var performer = ctx.Performer;
        var station = ctx.Station;

        if (HasComp<MalfAiThermalOverrideComponent>(station))
        {
            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.ThermalListing);
            _popup.PopupEntity(Loc.GetString(ThermalAlready), performer, performer);
            return;
        }

        EnsureComp<MalfAiThermalOverrideComponent>(station);

        _popup.PopupEntity(Loc.GetString(ThermalDone), performer, performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) disabled automatic fire alarms " +
            $"on station {station} (manual control intact).");
    }

    private void DoSilentRecords(MalfExecutionContext ctx)
    {
        ctx.Role.Comp2.SilentCriminalRecords = true;

        _popup.PopupEntity(Loc.GetString(SilentRecordsDone), ctx.Performer, ctx.Performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(ctx.Performer):player} (Malf AI) muted Security radio reports from criminal records.");
    }

    private void DoCameraUpgrade(MalfExecutionContext ctx)
    {
        var performer = ctx.Performer;
        var station = ctx.Station;

        if (HasComp<MalfAiCameraUpgradeComponent>(station))
        {
            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.CameraUpgradeListing);
            _popup.PopupEntity(Loc.GetString(CameraAlready), performer, performer);
            return;
        }

        var upgrade = EnsureComp<MalfAiCameraUpgradeComponent>(station);

        var seeds = new List<EntityUid>();
        var query = EntityQueryEnumerator<StationAiVisionComponent>();
        while (query.MoveNext(out var seedUid, out _))
        {
            if (_station.GetOwningStation(seedUid) != station)
                continue;

            seeds.Add(seedUid);
        }

        foreach (var seedUid in seeds)
            OverclockCamera(seedUid, upgrade.CameraRange);

        _malfAi.GrantThermalVision(performer, upgrade);

        _popup.PopupEntity(Loc.GetString(CameraDone, ("count", seeds.Count)), performer, performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) overclocked {seeds.Count} station vision seeds " +
            $"(x-ray, unpowered sight) and gained thermal vision on station {station}.");
    }

    private void OverclockCamera(EntityUid seedUid, float range)
    {
        if (!TryComp<StationAiVisionComponent>(seedUid, out var vision))
            return;

        _stationAi.SetVisionOverclock(seedUid, vision, range);

        if (TryComp<SurveillanceCameraComponent>(seedUid, out var camera))
        {
            _powerReceiver.SetNeedsPower(seedUid, false);
            _cameras.SetActive(seedUid, true, camera);
        }
    }

    private void OnCameraSpawned(Entity<StationAiVisionComponent> ent, ref ComponentStartup args)
    {
        var station = _station.GetOwningStation(ent);
        if (station == null || !TryComp<MalfAiCameraUpgradeComponent>(station.Value, out var upgrade))
            return;

        OverclockCamera(ent, upgrade.CameraRange);
    }

    private void OnAirFlood(Entity<MalfAiActorComponent> ent, ref MalfAiAirFloodEvent args)
    {
        if (args.Handled)
            return;

        if (!_malfAi.TryGetMalfContext(args.Performer, out var ctx))
            return;

        args.Handled = true;
        DoAirFlood(ctx.Value);
    }

    private void DoAirFlood(MalfExecutionContext ctx)
    {
        var performer = ctx.Performer;
        var station = ctx.Station;

        EnsureComp<MalfAiAirFloodComponent>(station);

        var flooded = 0;
        var query = EntityQueryEnumerator<AirAlarmComponent>();
        while (query.MoveNext(out var alarmUid, out var alarm))
        {
            if (_station.GetOwningStation(alarmUid) != station)
                continue;

            alarm.AutoMode = false;
            _airAlarm.SetMode(alarmUid, string.Empty, AirAlarmMode.Flood, false);
            flooded++;
        }

        _popup.PopupEntity(Loc.GetString(FloodDone, ("count", flooded)), performer, performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) unlocked and engaged air alarm Flood mode " +
            $"on station {station} ({flooded} alarms).");
    }

    private void OnChaosPulse(Entity<MalfAiActorComponent> ent, ref MalfAiChaosPulseEvent args)
    {
        if (args.Handled)
            return;

        if (!_malfAi.TryGetMalfContext(args.Performer, out var ctx))
            return;

        if (!TryComp<MalfAiChaosPulseComponent>(args.Action, out var pool) || pool.Events.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString(ChaosFailed), args.Performer, args.Performer);
            return;
        }

        var weighted = new Dictionary<EntProtoId, float>(pool.Events.Count);
        foreach (var entry in pool.Events)
        {
            if (entry.Weight <= 0f
                || !_proto.TryIndex(entry.Event, out var proto)
                || proto == null
                || !proto.Components.TryGetComponent("GameRule", out _))
            {
                continue;
            }

            weighted[entry.Event] = entry.Weight;
        }

        if (weighted.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString(ChaosFailed), args.Performer, args.Performer);
            return;
        }

        var picked = _random.Pick(weighted);

        var rule = _ticker.AddGameRule(picked.Id);
        if (TryComp<GameRuleComponent>(rule, out var ruleData))
            ruleData.Delay = null;

        if (!_ticker.StartGameRule(rule, ruleData))
        {
            QueueDel(rule);
            _popup.PopupEntity(Loc.GetString(ChaosFailed), args.Performer, args.Performer);
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString(ChaosDone, ("event", Name(rule))), args.Performer, args.Performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(ctx.Value.Performer):player} (Malf AI) started station event {picked.Id}.");
    }
}
