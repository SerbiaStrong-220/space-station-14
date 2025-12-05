using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class GhostBodyListResponse : EntityEventArgs
{
    public List<TrailPoint> Bodies { get; }
    public GhostBodyListResponse(List<TrailPoint> bodies)
    {
        Bodies = bodies;
    }
}
