using Content.Shared.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class StealthIFFConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public TimeSpan Cooldown;
    public TimeSpan StealthDuration;
}

[Serializable, NetSerializable]
public enum StealthIFFConsoleUiKey : byte
{
    Key,
}
