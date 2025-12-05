using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension;

[Serializable, NetSerializable]
public enum BodyStateToEnter
{
    Avaible,
    Abandoned,
    Engaged,
    InCryo,
    Destroyed
}
