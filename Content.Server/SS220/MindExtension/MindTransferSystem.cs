using Content.Shared.Ghost;
using Content.Shared.SS220.MindExtension;
using Content.Shared.SS220.MindExtension.Events;
using Robust.Shared.Network;

namespace Content.Server.SS220.MindExtension;

public partial class MindExtensionSystem : EntitySystem //MindTransferSystem
{
    #region Handlers
    private void OnMindTransferedEvent(ref MindTransferedEvent ev)
    {
        if (ev.Player is null)
            return;

        var mindExtEnt = GetMindExtension((NetUserId)ev.Player);

        var mindExt = mindExtEnt.Comp;

        //На всякий удалить старый MindContainer.
        if (TryComp<MindExtensionContainerComponent>(ev.OldEntity, out var oldMindExt))
        {
            ChangeOrAddTrailPoint(mindExt, (EntityUid)ev.OldEntity, CheckEntityAbandoned((EntityUid)ev.OldEntity));
            _entityManager.RemoveComponent<MindExtensionContainerComponent>((EntityUid)ev.OldEntity);
        }

        if (ev.NewEntity is null)
            return;

        ChangeOrAddTrailPoint(mindExt, (EntityUid)ev.NewEntity, false);
        SetRespawnTimer(mindExt, (EntityUid)ev.NewEntity, (NetUserId)ev.Player);

        var mindExtCont = new MindExtensionContainerComponent() { MindExtension = mindExtEnt.Owner };

        //Если компонент уже есть, просто заменить ему ИД на православный.
        if (TryComp<MindExtensionContainerComponent>(ev.NewEntity, out var newMindExt))
            newMindExt.MindExtension = mindExtCont.MindExtension;
        else
            _entityManager.AddComponent((EntityUid)ev.NewEntity, mindExtCont);
    }
    private void OnSuicidedEvent(ref SuicidedEvent ev)
    {
        Suicide(ev.Invoker, ev.Player);
    }
    private void OnGhostedEvent(ref GhostedEvent ev)
    {
        GhostAttempt(ev.OldEntity, ev.CanReturn);
    }
    #endregion

    public void GhostAttempt(EntityUid oldEntity, bool canReturn)
    {
        if (!TryComp<MindExtensionContainerComponent>(oldEntity, out var oldMindExt))
            return;

        if (!TryGetMindExtension(oldMindExt, out var mindExt))
            return;

        ChangeOrAddTrailPoint(mindExt, oldEntity, CheckEntityAbandoned(oldEntity));
    }
    public void Suicide(EntityUid invoker, NetUserId player)
    {
        // Если сущность жива, то делать нечего.
        if (!(_mobState.IsCritical(invoker) || _mobState.IsDead(invoker)))
            return;

        if (!TryGetMindExtension(player, out var mindExtEnt))
            return;

        ChangeOrAddTrailPoint(
            comp: mindExtEnt.Value.Comp,
            entity: invoker,
            // Все просто, сущность доступна при суициде в крите/после смерти.
            isAbandoned: false);
    }
}
