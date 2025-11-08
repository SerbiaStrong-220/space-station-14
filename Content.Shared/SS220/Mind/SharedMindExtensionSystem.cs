using Content.Shared.Ghost;
using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.Mind;
public sealed partial class SharedMindExtensionSystem : EntitySystem
{
    public void RespawnAction(NetEntity netEntity)
    {
        /*if (!TryGetEntity(netEntity, out var entity))
            return;*/

        var entity = new EntityUid(netEntity.Id);

        var msg = new RespawnActionEvent();
        RaiseLocalEvent((EntityUid)entity, msg);
    }
}
