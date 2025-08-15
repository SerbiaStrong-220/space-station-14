// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.Radio;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.StationRecords.Components;

namespace Content.Server.SS220.CriminalRecords;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly CriminalRecordSystem _criminalRecord = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    private static readonly TimeSpan CooldownLagTolerance = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        SubscribeLocalEvent<CriminalRecordsConsole220Component, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsole220Component, SelectStationRecord>(OnKeySelected);
        SubscribeLocalEvent<CriminalRecordsConsole220Component, UpdateCriminalRecordStatus>(OnCriminalStatusUpdate);
        SubscribeLocalEvent<CriminalRecordsConsole220Component, DeleteCriminalRecordStatus>(OnCriminalStatusDelete);
        SubscribeLocalEvent<RecordModifiedEvent>(OnRecordModified);
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnRecordCreated);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, ActivatableUIOpenAttemptEvent>(OnAttemptOpenUI);
    }

    private void OnAttemptOpenUI(Entity<GeneralStationRecordConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (_accessReader.IsAllowed(args.User, ent.Owner))
            return;

        _popup.PopupEntity(Loc.GetString("criminal-records-ui-no-access"), ent.Owner, recipient: args.User);
        args.Cancel();
    }

    private void OnRecordCreated(AfterGeneralRecordCreatedEvent args)
    {
        var query = EntityQueryEnumerator<CriminalRecordsConsole220Component>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_stationSystem.GetOwningStation(uid) == args.Station)
                UpdateUserInterface(uid, comp);
        }
    }

    private void OnRecordModified(RecordModifiedEvent args)
    {
        var query = EntityQueryEnumerator<CriminalRecordsConsole220Component>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_stationSystem.GetOwningStation(uid) == args.Station)
                UpdateUserInterface(uid, comp);
        }
    }

    private void UpdateUserInterface<T>(Entity<CriminalRecordsConsole220Component> ent, ref T ev)
    {
        UpdateUserInterface(ent);
    }

    private void OnKeySelected(Entity<CriminalRecordsConsole220Component> entity, ref SelectStationRecord msg)
    {
        entity.Comp.ActiveKey = msg.SelectedKey;
        _audio.PlayPvs(entity.Comp.KeySwitchSound, entity);
        UpdateUserInterface(entity, entity.Comp);
    }

    private void SendRadioMessage(EntityUid sender, string message, string channel)
    {
        _radio.SendRadioMessage(
            sender,
            message,
            _prototype.Index<RadioChannelPrototype>(channel),
            sender);
    }

    private StationRecordKey? TryGetConsoleActiveRecordKey(Entity<CriminalRecordsConsole220Component> entity)
    {
        var consoleStation = _stationSystem.GetOwningStation(entity);
        if (consoleStation is not { Valid: true })
            return null;

        if (!entity.Comp.ActiveKey.HasValue)
            return null;

        return new StationRecordKey(entity.Comp.ActiveKey.Value, consoleStation.Value);
    }

    private void OnCriminalStatusUpdate(Entity<CriminalRecordsConsole220Component> ent, ref UpdateCriminalRecordStatus args)
    {
        if (!ent.Comp.IsSecurity)
            return;

        if (TryGetConsoleActiveRecordKey(ent) is not { } key)
            return;

        if (!_accessReader.IsAllowed(args.Actor, ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("criminal-records-ui-no-access"), ent.Owner, recipient: args.Actor);
            return;
        }

        var currentTime = _gameTicker.RoundDuration();
        if (ent.Comp.LastEditTime != null && ent.Comp.LastEditTime + ent.Comp.EditCooldown - CooldownLagTolerance > currentTime)
        {
            _popup.PopupEntity(Loc.GetString("criminal-status-cooldown-popup"), ent.Owner, args.Actor);
            return;
        }

        var messageCut = args.Message;
        if (messageCut.Length > ent.Comp.MaxMessageLength)
            messageCut = messageCut.Substring(0, ent.Comp.MaxMessageLength);

        // get previous record so we can compare criminal status later
        _criminalRecord.TryGetLastRecord(key, out var generalRecord, out var prevRecord);

        if (!_criminalRecord.AddCriminalRecordStatus(key, messageCut, args.StatusTypeId, args.Actor))
            return;

        // compare criminal state and report on radio that it was changed
        if (generalRecord != null && args.StatusTypeId.HasValue)
        {
            if (prevRecord == null || prevRecord.RecordType != args.StatusTypeId)
            {
                if (_prototype.TryIndex(args.StatusTypeId.Value, out var status))
                {
                    if (!string.IsNullOrWhiteSpace(status.RadioReportMessage))
                    {
                        SendRadioMessage(
                            ent.Owner,
                            Loc.GetString(status.RadioReportMessage, ("target", generalRecord.Name), ("reason", messageCut)),
                            ent.Comp.ReportRadioChannel);
                    }
                }
            }
        }

        ent.Comp.LastEditTime = currentTime;
        _audio.PlayPvs(ent.Comp.DatabaseActionSound, ent.Owner);
    }

    private void OnCriminalStatusDelete(Entity<CriminalRecordsConsole220Component> entity, ref DeleteCriminalRecordStatus args)
    {
        if (!entity.Comp.IsSecurity)
            return;

        if (TryGetConsoleActiveRecordKey(entity) is not { } key)
            return;

        if (!_accessReader.IsAllowed(args.Actor, entity))
        {
            _popup.PopupEntity(Loc.GetString("criminal-records-ui-no-access"), entity.Owner, recipient: args.Actor);
            return;
        }

        if (!_criminalRecord.RemoveCriminalRecordStatus(key, args.Time, args.Actor))
            return;

        _audio.PlayPvs(entity.Comp.DatabaseActionSound, entity);
    }

    private void UpdateUserInterface(EntityUid uid, CriminalRecordsConsole220Component? console = null)
    {
        if (!Resolve(uid, ref console))
            return;

        var owningStation = _stationSystem.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecordsComponent))
        {
            CriminalRecordConsoleState state = new(null, null, null);
            SetStateForInterface(uid, state);
            return;
        }

        var consoleRecords =
            _stationRecords.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecordsComponent);

        var listing = new Dictionary<uint, CriminalRecordShort>();

        foreach (var (key, record) in consoleRecords)
        {
            var shortRecord = new CriminalRecordShort(record, console.IsSecurity);
            listing.Add(key, shortRecord);
        }

        if (listing.Count == 0)
            console.ActiveKey = null;

        GeneralStationRecord? selectedRecord = null;
        if (TryGetConsoleActiveRecordKey((uid, console)) is { } activeKey)
        {
            _stationRecords.TryGetRecord(
                activeKey,
                out selectedRecord,
                stationRecordsComponent);
        }

        CriminalRecordConsoleState newState = new(console.ActiveKey, selectedRecord, listing);
        SetStateForInterface(uid, newState);
    }

    private void SetStateForInterface(EntityUid uid, CriminalRecordConsoleState newState)
    {
        _userInterface.SetUiState(uid, CriminalRecordsUiKey.Key, newState);
    }
}
