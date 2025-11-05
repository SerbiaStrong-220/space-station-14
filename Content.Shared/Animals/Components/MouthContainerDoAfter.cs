using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Animals.Components;

[Serializable, NetSerializable]
public sealed partial class MouthContainerDoAfterEvent : SimpleDoAfterEvent
{
    [NonSerialized]
    [DataField("target", required: true)]
    public EntityUid ToInsert = default!;

    public MouthContainerDoAfterEvent(EntityUid toInsert)
    {
        ToInsert = toInsert;
    }
}
