using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MouthContainer;

[Serializable, NetSerializable]
public sealed partial class MouthContainerDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity ToInsert { get; private set; }

    public MouthContainerDoAfterEvent(NetEntity toInsert)
    {
        ToInsert = toInsert;
    }
}
