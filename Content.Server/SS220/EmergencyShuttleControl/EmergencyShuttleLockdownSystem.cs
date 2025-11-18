// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Pinpointer;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.EmergencyShuttleControl;
using Content.Shared.SS220.EmergencyShuttleControl.Lockdown;
using Content.Shared.Station.Components;
using JetBrains.FormatRipper.Elf;
using Microsoft.Extensions.DependencyModel;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.SS220.EmergencyShuttleControl;
/// <summary>
///     System that manages the cancellation of emergency shuttle call.
/// </summary>
public sealed class EmergencyShuttleLockdownSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    private const string AnnounceLocationLoc = "shuttle-lockdown-announce-locate";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmergencyShuttleLockdownComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EmergencyShuttleLockdownComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);

        SubscribeLocalEvent<EmergencyShuttleLockdownComponent, UseInHandEvent>(OnUseInHand);
    }

    #region Handlers
    private void OnMapInit(Entity<EmergencyShuttleLockdownComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.IsActivatedOnStartup)
            Activate(ent);
    }

    private void OnComponentShutdown(Entity<EmergencyShuttleLockdownComponent> ent, ref ComponentShutdown args)
    {
        Deactivate(ent);
    }

    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<EmergencyShuttleLockdownComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (!comp.IsActive)
                continue;

            ev.Cancelled = true;
            ev.Reason = Loc.GetString(comp.WarningMessage);
            break;
        }
    }

    private void OnUseInHand(Entity<EmergencyShuttleLockdownComponent> ent, ref UseInHandEvent e)
    {
        if (ent.Comp.IsInHandActive)
        {
            Toggle(ent);
        }
    }
    #endregion

    /// <summary>
    ///     Sets the component to the active state if it was previously deactivated.
    /// </summary>
    public void Activate(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        if (!ent.Comp.IsActive &&
            !_emergency.EmergencyShuttleArrived &&
            ValidateGridInStation(ent))
        {
            ent.Comp.IsActive = true;
            _roundEnd.CancelRoundEndCountdown(ent.Owner, false);

            var args = new EmergencyShuttleLockdownActivatedEvent();
            RaiseLocalEvent(ent, ref args, true);

            SendAnounce(ent);
        }
    }

    /// <summary>
    ///     Sets the component to the inactive state if it was previously activated.
    /// </summary>
    public void Deactivate(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        if (ent.Comp.IsActive &&
            !_emergency.EmergencyShuttleArrived)
        {
            ent.Comp.IsActive = false;

            var args = new EmergencyShuttleLockdownDeactivatedEvent();
            RaiseLocalEvent(ent, ref args, true);

            SendAnounce(ent);
        }
    }

    public void Toggle(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        if (ent.Comp.IsActive)
            Deactivate(ent);
        else
            Activate(ent);
    }

    /// <summary>
    ///     It's check <paramref name="ent"/> location at the any station
    /// </summary>
    private bool ValidateGridInStation(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        if (!ent.Comp.IsOnlyInStationActive)
            return true;

        EmergencyShuttleLockdownComponent comp = ent.Comp;

        var xform = Transform(ent.Owner);

        //If it's not on the grid, it's definitely not at the station.
        if (xform.GridUid is null)
            return false;

        foreach (var station in _station.GetStations())
        {
            var stationComponent = _entityManager.GetComponent<StationDataComponent>(station);
            var grids = stationComponent.Grids;

            if (grids.Contains((EntityUid)xform.GridUid))
                return true;
        }

        return false;
    }

    #region Announce

    private void SendAnounce(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        LocId? messageBody;

        if (ent.Comp.IsActive)
            messageBody = ent.Comp.OnActiveMessage;
        else
            messageBody = ent.Comp.OnDeactiveMessage;

        //If there is no message body, there should be no announce.
        if (messageBody is null)
            return;

        //If displaying coordinates is disabled, this should be empty.
        string position = "";
        if (ent.Comp.IsDisplayLocation && ent.Comp.IsDisplayCoordinates)
        {
            position = Loc.GetString(AnnounceLocationLoc,
                ("locationType", "both"),
                GetCoordinatesArgument(ent),
                GetLocationArgument(ent));
        }
        else if (ent.Comp.IsDisplayCoordinates)
        {
            position = Loc.GetString(AnnounceLocationLoc,
                ("locationType", "coords"),
                GetCoordinatesArgument(ent));
        }
        else if (ent.Comp.IsDisplayLocation)
        {
            position = Loc.GetString(AnnounceLocationLoc,
                ("locationType", "location"),
                GetLocationArgument(ent));
        }

        var announceMessage = Loc.GetString(messageBody,
            ("position", position));

        _chat.DispatchGlobalAnnouncement(
            message: announceMessage,
            sender: Loc.GetString(ent.Comp.AnnounceTitle),
            playSound: false,
            colorOverride: ent.Comp.AnnounceColor);

        SoundSpecifier announceSound;

        if (ent.Comp.IsActive)
            announceSound = ent.Comp.ActivateSound;
        else
            announceSound = ent.Comp.DeactiveSound;

        _audio.PlayGlobal(announceSound, Filter.Broadcast(), true);
    }
    private (string, object) GetCoordinatesArgument(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        var coordinates = _transform.GetWorldPosition(ent.Owner);
        return ("coords", $"({Math.Round(coordinates.X)}, {Math.Round(coordinates.Y)})");
    }
    private (string, object) GetLocationArgument(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        return ("location",
                FormattedMessage.RemoveMarkupOrThrow(
                    _navMap.GetNearestBeaconString((ent, Transform(ent.Owner)))));
    }

    #endregion
}
