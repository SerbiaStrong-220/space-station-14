using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AnnounceTTS;

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class AnnounceTTSEvent : EntityEventArgs
{
    public AnnounceTTSEvent(int id, byte[] data)
    {
        Id = id;
        Data = data;
    }

    public int Id { get; }
    public byte[] Data { get; }
}
