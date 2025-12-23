// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MindExtension;
using Robust.Shared.Network;

namespace Content.Server.SS220.MindExtension;

public partial class MindExtensionSystem : EntitySystem //MindTransferSystem
{
    /// <summary>
    /// Moves a MindExtension from one entity to another.
    /// This method implements the core logic of the entire system.
    /// </summary>
    public void TransferExtension(EntityUid? oldEntity, EntityUid? newEntity, NetUserId? player)
    {
        if (player is null)
            return;

        var mindExtEnt = GetMindExtension(player.Value);
        var mindExt = mindExtEnt.Comp;

        if (TryComp<MindExtensionContainerComponent>(oldEntity, out var oldMindExt))
        {
            //Main check for abandonment
            ChangeOrAddTrailPoint(mindExt, oldEntity.Value, CheckEntityAbandoned(oldEntity.Value));
            EntityManager.RemoveComponent<MindExtensionContainerComponent>(oldEntity.Value);
        }

        if (newEntity is null)
            return;

        ChangeOrAddTrailPoint(mindExt, newEntity.Value, false);
        SetRespawnTimer(mindExt, newEntity.Value, player.Value);

        var mindExtCont = new MindExtensionContainerComponent() { MindExtension = mindExtEnt.Owner };

        if (!TryComp<MindExtensionContainerComponent>(newEntity, out var newMindExt))
            newMindExt = EnsureComp<MindExtensionContainerComponent>(newEntity.Value);

        newMindExt.MindExtension = mindExtEnt.Owner;
    }

    /// <summary>
    /// Marks the entity in the system as not abandoned.
    /// This is necessary for suicide or other mind transfer methods that allow
    /// the player to return despite the primary check.
    /// </summary>
    public void MarkAsNotAbandoned(EntityUid invoker, NetUserId player)
    {
        if (!TryGetMindExtension(player, out var mindExtEnt))
            return;

        ChangeOrAddTrailPoint(
            comp: mindExtEnt.Value.Comp,
            entity: invoker,
            isAbandoned: false);
    }
}
