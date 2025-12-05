using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class UpdateRespawnTimeMessage : EntityEventArgs
{
    public TimeSpan Time;

    public UpdateRespawnTimeMessage(TimeSpan time)
    {
        Time = time;
    }
}
