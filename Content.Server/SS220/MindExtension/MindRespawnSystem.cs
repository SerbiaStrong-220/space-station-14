// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Forensics.Components;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.MindExtension;
using Content.Shared.SS220.MindExtension.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.MindExtension;

public partial class MindExtensionSystem : EntitySystem //MindRespawnSystem
{
    private void SubscribeRespawnSystemEvents()
    {
        SubscribeNetworkEvent<ExtensionRespawnActionEvent>(OnRespawnActionEvent);
        SubscribeNetworkEvent<RespawnTimeRequest>(OnRespawnTimeRequest);
    }

    private void OnRespawnActionEvent(ExtensionRespawnActionEvent ev, EntitySessionEventArgs args)
    {
        if (!TryComp<MindExtensionContainerComponent>(args.SenderSession.AttachedEntity, out var mindContExt))
            return;

        if (!TryComp<MindExtensionComponent>(mindContExt.MindExtension, out var mindExt))
            return;

        if (!mindExt.RespawnAvailable)
            return;

        if (_gameTiming.CurTime > mindExt.RespawnTimer)
            RaiseLocalEvent(args.SenderSession.AttachedEntity.Value, new RespawnActionEvent());
    }

    private void OnRespawnTimeRequest(RespawnTimeRequest ev, EntitySessionEventArgs args)
    {
        if (!TryGetMindExtension(args.SenderSession.UserId, out var mindExtEnt))
            return;

        UpdateRespawnTimer(mindExtEnt.Value.Comp.RespawnTimer, args.SenderSession);
    }

    private void SetRespawnTimer(MindExtensionComponent component, EntityUid newEntity, NetUserId playerId)
    {
        //This is the main check to see if transferring to this entity resets the return timer to the lobby.
        //Now the timer is only turned off when transferring to a living humanoid (Dna Component) or Borg.
        if (HasComp<DnaComponent>(newEntity) || HasComp<BorgChassisComponent>(newEntity))
        {
            if (TryComp<MobStateComponent>(newEntity, out var mobState) &&
                (mobState.CurrentState == MobState.Dead || mobState.CurrentState == MobState.Invalid))
            {
                ChangeRespawnAvaible(component, playerId, true);
            }

            ChangeRespawnAvaible(component, playerId, false);
        }
        else
            ChangeRespawnAvaible(component, playerId, true);
    }

    private void ChangeRespawnAvaible(MindExtensionComponent component, NetUserId playerId, bool value)
    {
        if (value && !component.RespawnAvailable)
        {
            component.RespawnTimer = _gameTiming.CurTime + component.RespawnTime;
            UpdateRespawnTimer(component.RespawnTimer, _playerManager.GetSessionById(playerId));
        }

        //When turned off, set the value to false, which means the timer is turned off.
        if (!value && component.RespawnAvailable)
        {
            component.RespawnTimer = null;
            UpdateRespawnTimer(component.RespawnTimer, _playerManager.GetSessionById(playerId));
        }

        component.RespawnAvailable = value;
    }

    private void UpdateRespawnTimer(TimeSpan? timer, ICommonSession session)
    {
        var ev = new RespawnTimeResponse(timer);
        RaiseNetworkEvent(ev, session);
    }
}
