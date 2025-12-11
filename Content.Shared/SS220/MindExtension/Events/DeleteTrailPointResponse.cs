using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class DeleteTrailPointResponse : EntityEventArgs
{
    public NetEntity Entity { get; }

    public DeleteTrailPointResponse(NetEntity entity)
    {
        Entity = entity;
    }
}
