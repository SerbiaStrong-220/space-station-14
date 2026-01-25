using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MouthContainer;

[Serializable, NetSerializable]
public sealed partial class MouthContainerDoAfterInsertEvent : SimpleDoAfterEvent
{
    public readonly NetEntity ToInsert;

    public MouthContainerDoAfterInsertEvent(NetEntity toInsert)
    {
        ToInsert = toInsert;
    }
}
[Serializable, NetSerializable]
public sealed partial class MouthContainerDoAfterEjectEvent : SimpleDoAfterEvent;
