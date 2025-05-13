using Content.Shared.Radio.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.EntitySystems;

public sealed class GetInsteadIdCardNameEvent : EntityEventArgs
{
    public EntityUid Uid;
    public string? Name;

    public GetInsteadIdCardNameEvent(EntityUid uid)
    {
        Uid = uid;
    }
}
