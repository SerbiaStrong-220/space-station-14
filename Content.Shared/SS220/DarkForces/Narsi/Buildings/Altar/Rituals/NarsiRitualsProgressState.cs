using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Rituals;

[Serializable, NetSerializable]
public enum NarsiRitualsProgressState
{
    Idle,
    Working,
    Delay
}
