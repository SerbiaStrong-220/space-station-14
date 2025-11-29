using Content.Shared.Forensics.Components;
using Content.Shared.Ghost;
using Content.Shared.SS220.GhostExtension;
using Content.Shared.SS220.Mind;
using Robust.Shared.Network;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace Content.Server.SS220.MindExtension;

public sealed class MindRespawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ExtensionRespawnActionEvent>(OnRespawnActionEvent);

        //TODO: Установить связь таймера с UI. Выявлять суицидника.
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

        if (!mindExt.IsIC && mindExt.RespawnTimer < _gameTiming.CurTime)
            RaiseLocalEvent((EntityUid)args.SenderSession.AttachedEntity, new RespawnActionEvent());
    }

    public void SetRespawnTimer(MindExtensionComponent component, EntityUid newEntity, NetUserId session)
    {
        if (!TryComp<DnaComponent>(newEntity, out var dna))
        {
            if (component.IsIC == true)
            {
                component.RespawnTimer = _gameTiming.CurTime + TimeSpan.FromSeconds(component.RespawnAccumulatorMax);
                var ev = new UpdateRespawnTime((TimeSpan)component.RespawnTimer);
                RaiseNetworkEvent(ev, _playerManager.GetSessionById(session));
            }

            component.IsIC = false;
        }
        else
            component.IsIC = true;
    }
}
