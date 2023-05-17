using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AnnounceTTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class AnnounceTTSEvent : EntityEventArgs
{
    public AnnounceTTSEvent(int id, byte[] data, int delayMs)
    {
        Id = id;
        Data = data;
        DelayMs = delayMs;
    }

    public int Id { get; }
    public byte[] Data { get; }
    /// <summary>
    /// Delay in microseconds
    /// </summary>
    public int DelayMs { get; } = 0;
}
