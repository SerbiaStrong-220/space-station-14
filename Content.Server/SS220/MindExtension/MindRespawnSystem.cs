// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Forensics.Components;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.MindExtension;
using Content.Shared.SS220.MindExtension.Events;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.MindExtension;

public partial class MindExtensionSystem //MindRespawnSystem
{
    private void SubscribeRespawnSystemEvents()
    {
        SubscribeNetworkEvent<RespawnRequest>(OnRespawnRequest);
        SubscribeNetworkEvent<RespawnTimeRequest>(OnRespawnTimeRequest);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged; // we need to force update timer
    }

    private void OnRespawnRequest(RespawnRequest ev, EntitySessionEventArgs args)
    {
        if (!TryGetExtension(args.SenderSession.UserId, out var data))
            return;

        if (!data.RespawnAvailable)
            return;

        if (data.RespawnTimer != null && !(_gameTiming.CurTime > data.RespawnTimer))
            return;

        if (args.SenderSession.AttachedEntity == null)
            return;

        RaiseLocalEvent(args.SenderSession.AttachedEntity.Value, new RespawnActionEvent());
        RaiseNetworkEvent(new RespawnedResponse(), args.SenderSession);
    }

    private void OnRespawnTimeRequest(RespawnTimeRequest ev, EntitySessionEventArgs args)
    {
        if (!TryGetExtension(args.SenderSession.UserId, out var data))
            return;

        UpdateRespawnTimer(data.RespawnTimer, args.SenderSession);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs ev)
    {
        if (ev.NewStatus != SessionStatus.InGame)
            return;

        if (TryGetExtension(ev.Session.UserId, out var data))
            UpdateRespawnTimer(data.RespawnTimer, ev.Session);
    }

    private void SetRespawnTimer(MindExtensionData data, EntityUid newEntity, NetUserId playerId)
    {
        if (HasComp<DnaComponent>(newEntity) || HasComp<BorgChassisComponent>(newEntity))
        {
            if (TryComp<MobStateComponent>(newEntity, out var mobState) &&
                mobState.CurrentState is MobState.Dead or MobState.Invalid)
            {
                SetRespawnAvailable(data, playerId, true);
                return;
            }

            SetRespawnAvailable(data, playerId, false);
        }
        else
            SetRespawnAvailable(data, playerId, true);
    }

    /// <summary>
    /// Updates the respawn availability flag and calculates the cooldown timer.
    /// Only sets a timer if the player is actively in-game.
    /// </summary>
    private void SetRespawnAvailable(MindExtensionData data, NetUserId playerId, bool newAvailability)
    {
        data.RespawnAvailable = newAvailability;

        var session = _playerManager.GetSessionById(playerId);
        var shouldSendUpdate = data.RespawnAvailable != newAvailability;

        if (data.RespawnAvailable)
        {
            if (session.Status != SessionStatus.InGame)
            {
                data.RespawnTimer = null;
            }
            else if (data.RespawnTimer == null)
            {
                data.RespawnTimer = _gameTiming.CurTime + data.RespawnTime;
                shouldSendUpdate = true;
            }
        }
        else
        {
            if (data.RespawnTimer != null)
                shouldSendUpdate = true;

            data.RespawnTimer = null;
        }

        if (shouldSendUpdate)
            UpdateRespawnTimer(data.RespawnTimer, session);
    }

    /// <summary>
    /// Sends a network event to the client with the current respawn timer value.
    /// </summary>
    private void UpdateRespawnTimer(TimeSpan? timer, ICommonSession session)
    {
        RaiseNetworkEvent(new RespawnTimeResponse(timer), session);
    }
}
