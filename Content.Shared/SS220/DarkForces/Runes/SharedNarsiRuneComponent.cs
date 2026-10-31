using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Runes;

public abstract partial class SharedNarsiRuneComponent : Component
{
}

[Serializable, NetSerializable]
public enum RuneState
{
    Idle,
    Active,
}

[Serializable, NetSerializable]
public enum RuneStatus
{
    Status,
}