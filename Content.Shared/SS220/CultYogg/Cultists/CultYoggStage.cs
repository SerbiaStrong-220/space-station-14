using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.Cultists;

// Please keep this sequential (0, 1, 2, etc) because +1 is used to determine next stage.
[Serializable, NetSerializable]
public enum CultYoggStage
{
    Initial,
    Reveal,
    Alarm,
    God,
}
