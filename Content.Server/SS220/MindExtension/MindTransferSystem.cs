using Content.Shared.SS220.MindExtension;
using Robust.Shared.Network;

namespace Content.Server.SS220.MindExtension;

public partial class MindExtensionSystem : EntitySystem //MindTransferSystem
{
    private void SubscribeTransferSystemEvents()
    {
        SubscribeLocalEvent<MindTransferedEvent>(OnMindTransferedEvent);
        SubscribeLocalEvent<SuicidedEvent>(OnSuicidedEvent);
        SubscribeLocalEvent<GhostedEvent>(OnGhostedEvent);
    }

    #region Handlers
    private void OnMindTransferedEvent(ref MindTransferedEvent ev)
    {
        if (ev.Player is null)
            return;

        var mindExtEnt = GetMindExtension(ev.Player.Value);

        var mindExt = mindExtEnt.Comp;

        //На всякий удалить старый MindContainer.
        if (TryComp<MindExtensionContainerComponent>(ev.OldEntity, out var oldMindExt))
        {
            ChangeOrAddTrailPoint(mindExt, ev.OldEntity.Value, CheckEntityAbandoned(ev.OldEntity.Value));
            _entityManager.RemoveComponent<MindExtensionContainerComponent>(ev.OldEntity.Value);
        }

        if (ev.NewEntity is null)
            return;

        ChangeOrAddTrailPoint(mindExt, ev.NewEntity.Value, false);
        SetRespawnTimer(mindExt, ev.NewEntity.Value, ev.Player.Value);

        var mindExtCont = new MindExtensionContainerComponent() { MindExtension = mindExtEnt.Owner };

        //Если компонент уже есть, просто заменить ему ИД на православный.
        if (TryComp<MindExtensionContainerComponent>(ev.NewEntity, out var newMindExt))
            newMindExt.MindExtension = mindExtCont.MindExtension;
        else
            _entityManager.AddComponent(ev.NewEntity.Value, mindExtCont);
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
        // P.S. Даже если сущность жива, делать есть чего.
        /*if (!(_mobState.IsCritical(invoker) || _mobState.IsDead(invoker)))
            return;*/

        if (!TryGetMindExtension(player, out var mindExtEnt))
            return;

        ChangeOrAddTrailPoint(
            comp: mindExtEnt.Value.Comp,
            entity: invoker,
            // Все просто, сущность доступна при суициде в крите/после смерти.
            isAbandoned: false);
    }
}
