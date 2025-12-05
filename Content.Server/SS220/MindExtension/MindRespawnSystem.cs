using Content.Shared.Forensics.Components;
using Content.Shared.Ghost;
using Robust.Shared.Network;
using Robust.Server.Player;
using Robust.Shared.Timing;
using Content.Shared.SS220.MindExtension.Events;
using Content.Shared.SS220.MindExtension;

namespace Content.Server.SS220.MindExtension;

public partial class MindExtensionSystem : EntitySystem //MindRespawnSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;


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

        if (!mindExt.IsIC && mindExt.RespawnTimer < _gameTiming.CurTime)
            RaiseLocalEvent((EntityUid)args.SenderSession.AttachedEntity, new RespawnActionEvent());
    }

    private void SetRespawnTimer(MindExtensionComponent component, EntityUid newEntity, NetUserId session)
    {
        if (!TryComp<DnaComponent>(newEntity, out var dna))
        {
            if (component.IsIC == true)
            {
                component.RespawnTimer = _gameTiming.CurTime + TimeSpan.FromSeconds(component.RespawnTime);
                var ev = new UpdateRespawnTimeMessage((TimeSpan)component.RespawnTimer);
                RaiseNetworkEvent(ev, _playerManager.GetSessionById(session));
            }

            component.IsIC = false;
        }
        else
            component.IsIC = true;
    }
}
