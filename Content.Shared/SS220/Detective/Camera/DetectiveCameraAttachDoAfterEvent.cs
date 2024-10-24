// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Detective.Camera;

[Serializable, NetSerializable]
public sealed partial class DetectiveCameraAttachDoAfterEvent : SimpleDoAfterEvent
{
    [NonSerialized]
    [DataField("attachTarget", required: true)]
    public EntityUid AttachTarget;

    public DetectiveCameraAttachDoAfterEvent(EntityUid owner)
    {
        AttachTarget = owner;
    }
}
