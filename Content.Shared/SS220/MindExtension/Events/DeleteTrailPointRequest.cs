using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class DeleteTrailPointRequest : EntityEventArgs
{
    public NetEntity Entity { get; }

    public DeleteTrailPointRequest(NetEntity entity)
    {
        Entity = entity;
    }
}
