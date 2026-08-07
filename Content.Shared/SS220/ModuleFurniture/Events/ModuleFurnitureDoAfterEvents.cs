// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ModuleFurniture.Events;

[Serializable, NetSerializable]
public sealed partial class InsertedFurniturePart : SimpleDoAfterEvent
{
    public Vector2i Offset { get; }

    public InsertedFurniturePart(Vector2i offset) : base()
    {
        Offset = offset;
    }
}

[Serializable, NetSerializable]
public sealed partial class RemoveFurniturePartEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class DeconstructFurnitureEvent : SimpleDoAfterEvent { }
