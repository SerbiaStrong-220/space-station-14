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
        {
            return;
        }

        if (!TryComp<MindExtensionComponent>(mindContExt.MindExtension, out var mindExt))
        {
            return;
        }

        if (!mindExt.RespawnAvaible && mindExt.RespawnTimer < _gameTiming.CurTime)
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
        //Если возможность респавна появилась, запустить таймер.
        if (value && !component.RespawnAvaible)
        {
            component.RespawnTimer = _gameTiming.CurTime + component.RespawnTime;
            UpdateRespawnTimer(component.RespawnTimer, _playerManager.GetSessionById(playerId));
        }

        //Если возможность респавна пропала, таймер остановить.
        if (!value && component.RespawnAvaible)
        {
            component.RespawnTimer = null;
            UpdateRespawnTimer(component.RespawnTimer, _playerManager.GetSessionById(playerId));
        }

        component.RespawnAvaible = value;
    }

    private void UpdateRespawnTimer(TimeSpan? timer, ICommonSession session)
    {
        var ev = new RespawnTimeResponse(timer);
        RaiseNetworkEvent(ev, session);
    }
}
