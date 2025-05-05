using Robust.Shared.Serialization;

namespace Content.Shared.SS220.PowerCellFour;

[Serializable, NetSerializable]
public enum PowerCellFourVisual : byte
{
    State,
}

[Flags]
[Serializable, NetSerializable]
public enum PowerCellFourVisualStates : byte
{
    Base,
    First,
    Second,
    Third,
    Fourth,
}
