using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class RespawnTimeResponse : EntityEventArgs
{
    public TimeSpan? Time;

    public RespawnTimeResponse(TimeSpan? time)
    {
        Time = time;
    }
}
