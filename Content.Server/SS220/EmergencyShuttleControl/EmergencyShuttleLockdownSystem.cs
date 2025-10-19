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
using Microsoft.Extensions.DependencyModel;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Linq;

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);

        SubscribeLocalEvent<EmergencyShuttleLockdownComponent, EmergencyShuttleLockdownToggleActionEvent>(OnEmergencyShuttleLockdownToggleAction);
        SubscribeLocalEvent<EmergencyShuttleLockdownComponent, EmergencyShuttleLockdownActivateActionEvent>(OnEmergencyShuttleLockdownActivateActionEvent);
        SubscribeLocalEvent<EmergencyShuttleLockdownComponent, EmergencyShuttleLockdownDeactivateActionEvent>(OnEmergencyShuttleLockdownDeactivateActionEvent);

        SubscribeLocalEvent<EmergencyShuttleLockdownComponent, UseInHandEvent>(OnUseInHand);
    }

    #region Handlers
    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent ev)
    {
        var lockdowns = _entityManager.AllComponents<EmergencyShuttleLockdownComponent>();
        var temp = lockdowns.Where(x => x.Component.IsActivated);
        if (temp.Count() > 0)
        {
            ev.Cancelled = true;
            ev.Reason = Loc.GetString(temp.First().Component.WarningMessage);
            return;
        }
    }

    private void OnEmergencyShuttleLockdownToggleAction(Entity<EmergencyShuttleLockdownComponent> ent, ref EmergencyShuttleLockdownToggleActionEvent args)
    {
        if (ent.Comp.IsActivated)
            Deactivate(ent);
        else
            Activate(ent);
    }

    private void OnEmergencyShuttleLockdownDeactivateActionEvent(
        Entity<EmergencyShuttleLockdownComponent> ent,
        ref EmergencyShuttleLockdownDeactivateActionEvent args)
    {
        Activate(ent);
    }

    private void OnEmergencyShuttleLockdownActivateActionEvent(
        Entity<EmergencyShuttleLockdownComponent> ent,
        ref EmergencyShuttleLockdownActivateActionEvent args)
    {
        Deactivate(ent);
    }

    private void OnUseInHand(Entity<EmergencyShuttleLockdownComponent> entity, ref UseInHandEvent e)
    {
        if (entity.Comp.IsInHandActive)
        {
            var args = new EmergencyShuttleLockdownToggleActionEvent();
            RaiseLocalEvent(entity, ref args);
        }
    }
    #endregion

    private void Activate(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        if (!_emergency.EmergencyShuttleArrived && ValidateGridInStation(ent))
        {
            ent.Comp.IsActivated = true;
            _roundEnd.CancelRoundEndCountdown(ent.Owner, false);

            var args = new EmergencyShuttleLockdownActiveEvent();
            RaiseLocalEvent(ent, ref args);

            SendAnounce(ent);
        }
    }

    private void Deactivate(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        if (!_emergency.EmergencyShuttleArrived && ValidateGridInStation(ent))
        {
            ent.Comp.IsActivated = false;

            var args = new EmergencyShuttleLockdownActiveEvent();
            RaiseLocalEvent(ent, ref args);

            SendAnounce(ent);
        }
    }

    private void SendAnounce(Entity<EmergencyShuttleLockdownComponent> ent)
    {
        LocId? messageBody;

        if (ent.Comp.IsActivated)
            messageBody = ent.Comp.OnActiveMessage;
        else
            messageBody = ent.Comp.OnDeactiveMessage;

        //If there is no message body, there should be no announce.
        if (messageBody is null)
            return;

        //If displaying coordinates is disabled, this should be empty.
        string position = "";
        if (ent.Comp.IsDisplayLocation || ent.Comp.IsDisplayCoordinates)
        {
            position += "\n" + Loc.GetString("shuttle-lockdown-announce-locate");

            if (ent.Comp.IsDisplayLocation)
                position += FormattedMessage.RemoveMarkupOrThrow(
                    _navMap.GetNearestBeaconString((ent, Transform(ent.Owner))));

            if (ent.Comp.IsDisplayCoordinates)
            {
                var coordinates = _transform.GetWorldPosition(ent.Owner);
                position += " " + Loc.GetString("shuttle-lockdown-announce-locate-coordinates",
                    ("coordinates", $" ({coordinates.X}, {coordinates.Y})"));
            }
        }

        var announceMessage = Loc.GetString(messageBody,
            ("position", position));

        _chat.DispatchGlobalAnnouncement(
            message: announceMessage,
            sender: Loc.GetString(ent.Comp.AnnounceTitle),
            playSound: false,
            colorOverride: ent.Comp.AnnounceColor);

        string announceAudioPath;

        if (ent.Comp.IsActivated)
            announceAudioPath = ent.Comp.OnActiveAudioPath;
        else
            announceAudioPath = ent.Comp.OnDeactiveAudioPath;

        _audio.PlayGlobal(new ResolvedPathSpecifier(announceAudioPath), Filter.Broadcast(), true);
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
}
