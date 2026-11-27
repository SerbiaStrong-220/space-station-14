// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.EntitySystems;
using Content.Server.Administration.Logs;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.Database;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Electrocution;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Prototypes;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.MalfAI;

public sealed partial class MalfAiLockdownSystem : StationEventSystem<MalfAiLockdownComponent>
{
    [Dependency] private SharedDoorSystem _doors = default!;
    [Dependency] private SharedElectrocutionSystem _electrify = default!;
    [Dependency] private SharedFirelockSystem _firelocks = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private MalfAiSystem _malfAi = default!;
    [Dependency] private PowerReceiverSystem _power = default!;

    private static readonly EntProtoId LockdownRuleProto = "MalfAiLockdownRule";

    private static readonly LocId LockdownActive = "malfai-lockdown-active";
    private static readonly LocId ModuleRefunded = "malfai-module-refunded";
    private static readonly LocId LockdownDone = "malfai-lockdown-done";

    private static readonly LocId StartAnnouncement = "malfai-lockdown-start-announcement";
    private static readonly LocId EndAnnouncement = "malfai-lockdown-end-announcement";

    private static readonly SoundSpecifier LockdownAnnounceSound =
        new SoundPathSpecifier("/Audio/Announcements/attention.ogg")
        {
            Params = AudioParams.Default.WithVolume(-4f),
        };

    private readonly Dictionary<EntityUid, EntityUid> _activeRules = new();
    [Dependency] private EntityQuery<MalfAiLockdownRestoreComponent> _restoreQuery = default!;
    [Dependency] private EntityQuery<AirlockComponent> _airlockQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MalfModuleExecuteEvent>(OnLockdownModule);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<MalfAiLockdownComponent, ComponentShutdown>(OnRuleShutdown);
        SubscribeLocalEvent<DoorComponent, DoorStateChangedEvent>(OnDoorStateChanged);
        SubscribeLocalEvent<MalfAiLockdownRestoreComponent, PowerChangedEvent>(OnRestorePowerChanged,
            after: [typeof(AirlockSystem), typeof(DoorSystem)]);
        SubscribeLocalEvent<MalfAiLockdownRestoreComponent, DoorBoltsChangedEvent>(OnRestoreBoltsChanged);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _activeRules.Clear();
    }

    private void AnnounceToStation(EntityUid station, LocId textKey, bool playSound)
    {
        if (!TryComp<StationDataComponent>(station, out var stationData))
            return;

        var filter = StationSystem.GetInStation(stationData);
        ChatSystem.DispatchFilteredAnnouncement(filter, Loc.GetString(textKey), playSound: playSound,
            announcementSound: LockdownAnnounceSound, colorOverride: Color.Gold, playTTS: false);
    }

    private void OnLockdownModule(ref MalfModuleExecuteEvent args)
    {
        if (args.ListingId != MalfAiConstants.LockdownListing)
            return;

        args.Handled = true;
        var ctx = args.Context;

        if (_activeRules.ContainsKey(ctx.Station))
        {
            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.LockdownListing);
            _popup.PopupEntity(Loc.GetString(LockdownActive), ctx.Performer, ctx.Performer);
            return;
        }

        var rule = _ticker.AddGameRule(LockdownRuleProto);
        EnsureComp<MalfAiLockdownComponent>(rule).Station = ctx.Station;
        if (!_ticker.StartGameRule(rule))
        {
            QueueDel(rule);
            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.LockdownListing);
            _popup.PopupEntity(Loc.GetString(ModuleRefunded), ctx.Performer, ctx.Performer);
            return;
        }

        _popup.PopupEntity(Loc.GetString(LockdownDone), ctx.Performer, ctx.Performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(ctx.Performer):player} (Malf AI) started hostile lockdown on station {ctx.Station}.");
    }

    protected override void Started(EntityUid uid, MalfAiLockdownComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (component.Station is not { } station)
        {
            _ticker.EndGameRule(uid, gameRule);
            return;
        }

        _activeRules[station] = uid;
        AnnounceToStation(station, StartAnnouncement, playSound: true);

        var query = EntityQueryEnumerator<DoorComponent, TransformComponent>();
        while (query.MoveNext(out var doorUid, out var door, out _))
        {
            if (StationSystem.GetOwningStation(doorUid) != station)
                continue;

            ApplyToDoor(component, (doorUid, door));
        }
    }

    private void ApplyToDoor(MalfAiLockdownComponent lockdown, Entity<DoorComponent> door)
    {
        var doorUid = door.Owner;
        if (_restoreQuery.HasComp(doorUid))
            return;

        if (HasComp<FirelockComponent>(doorUid))
        {
            if (door.Comp.State == DoorState.Open && _firelocks.EmergencyPressureStop(doorUid))
                lockdown.Closed.Add(doorUid);
            return;
        }

        if (!_airlockQuery.HasComp(doorUid))
        {
            if (door.Comp.State != DoorState.Closed && _doors.TryClose(doorUid, door.Comp))
                lockdown.Closed.Add(doorUid);
            return;
        }

        if (door.Comp.State != DoorState.Closed && _doors.TryClose(doorUid, door.Comp))
            lockdown.Closed.Add(doorUid);

        SecureAirlock(lockdown, doorUid);
    }

    private void OnDoorStateChanged(Entity<DoorComponent> ent, ref DoorStateChangedEvent args)
    {
        if (_restoreQuery.TryComp(ent, out var restore))
        {
            if (!restore.Restoring)
            {
                if (args.State == DoorState.Closed)
                    RestoreDoor((ent.Owner, restore));
                else
                    RemComp<MalfAiLockdownRestoreComponent>(ent);
            }
            return;
        }

        if (_activeRules.Count == 0 || args.State != DoorState.Closed)
            return;

        var station = StationSystem.GetOwningStation(ent);
        if (station == null || !_activeRules.TryGetValue(station.Value, out var rule))
            return;

        if (!TryComp<MalfAiLockdownComponent>(rule, out var lockdown))
            return;

        if (!_airlockQuery.HasComp(ent) || lockdown.Bolted.Contains(ent.Owner))
            return;

        SecureAirlock(lockdown, ent);
    }

    private void SecureAirlock(MalfAiLockdownComponent lockdown, EntityUid doorUid)
    {
        if (TryComp<ElectrifiedComponent>(doorUid, out var electrified) && !electrified.Enabled)
        {
            _electrify.SetElectrified((doorUid, electrified), true);
            lockdown.Electrified.Add(doorUid);
        }

        if (TryComp<DoorComponent>(doorUid, out var door)
            && door.State == DoorState.Closed
            && TryComp<DoorBoltComponent>(doorUid, out var bolts)
            && !bolts.BoltsDown
            && _doors.TrySetBoltDown((doorUid, bolts), true))
        {
            lockdown.Bolted.Add(doorUid);
        }
    }

    private void Revert(MalfAiLockdownComponent component)
    {
        foreach (var doorUid in component.Electrified)
        {
            if (TerminatingOrDeleted(doorUid))
                continue;
            if (TryComp<ElectrifiedComponent>(doorUid, out var electrified) && electrified.Enabled)
                _electrify.SetElectrified((doorUid, electrified), false);
        }

        var doors = new HashSet<EntityUid>(component.Bolted);
        doors.UnionWith(component.Closed);
        foreach (var doorUid in doors)
        {
            if (TerminatingOrDeleted(doorUid)
                || !TryComp<DoorComponent>(doorUid, out var door)
                || door.State is not (DoorState.Closed or DoorState.Closing))
                continue;

            var restore = EnsureComp<MalfAiLockdownRestoreComponent>(doorUid);
            restore.Unbolt = component.Bolted.Contains(doorUid);
            restore.Open = component.Closed.Contains(doorUid);
            RestoreDoor((doorUid, restore));
        }

        component.Bolted.Clear();
        component.Electrified.Clear();
        component.Closed.Clear();
    }

    private void OnRestorePowerChanged(Entity<MalfAiLockdownRestoreComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            RestoreDoor(ent);
    }

    private void OnRestoreBoltsChanged(Entity<MalfAiLockdownRestoreComponent> ent, ref DoorBoltsChangedEvent args)
    {
        if (!ent.Comp.Restoring)
            RemComp<MalfAiLockdownRestoreComponent>(ent);
    }

    private void RestoreDoor(Entity<MalfAiLockdownRestoreComponent> ent)
    {
        if (TerminatingOrDeleted(ent.Owner) || ent.Comp.Restoring)
            return;

        if (!TryComp<DoorComponent>(ent.Owner, out var door)
            || door.State is not (DoorState.Closed or DoorState.Closing)
            || TryComp<DoorBoltComponent>(ent.Owner, out var wiredBolts) && wiredBolts.BoltWireCut)
        {
            RemComp<MalfAiLockdownRestoreComponent>(ent);
            return;
        }

        if (!_power.IsPowered(ent.Owner) || door.State == DoorState.Closing)
            return;

        ent.Comp.Restoring = true;
        if (ent.Comp.Unbolt && TryComp<DoorBoltComponent>(ent.Owner, out var bolts) && bolts.BoltsDown)
            _doors.TrySetBoltDown((ent.Owner, bolts), false);

        if (ent.Comp.Open)
            _doors.TryOpen(ent.Owner, door);

        RemComp<MalfAiLockdownRestoreComponent>(ent);
    }

    protected override void Ended(EntityUid uid, MalfAiLockdownComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.Station is { } station)
        {
            _activeRules.Remove(station);
            AnnounceToStation(station, EndAnnouncement, playSound: false);
        }

        Revert(component);
    }

    private void OnRuleShutdown(Entity<MalfAiLockdownComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Station is { } station
            && _activeRules.TryGetValue(station, out var rule)
            && rule == ent.Owner)
        {
            _activeRules.Remove(station);
        }

        Revert(ent.Comp);
    }
}
