using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Photography;

[Serializable, NetSerializable]
public sealed class PhotoReservedMapMessage : EntityEventArgs
{
    public readonly MapId? Map;

    public PhotoReservedMapMessage(MapId? map)
    {
        Map = map;
    }
}

[Serializable, NetSerializable]
public sealed class PhotoRequestMapMessage : EntityEventArgs
{
}
