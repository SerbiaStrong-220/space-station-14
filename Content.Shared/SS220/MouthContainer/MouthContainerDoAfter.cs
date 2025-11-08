using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MouthContainer;

[Serializable, NetSerializable]
public sealed partial class MouthContainerDoAfterEvent : SimpleDoAfterEvent
{
    [NonSerialized]
    public EntityUid ToInsert;

    public MouthContainerDoAfterEvent(EntityUid toInsert)
    {
        ToInsert = toInsert;
    }
}
