using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class MindTrailRequestEvent : EntityEventArgs
{
    public HashSet<NetEntity> Trail { get; }
    public MindTrailRequestEvent(HashSet<NetEntity> trail)
    {
        Trail = trail;
    }
}
