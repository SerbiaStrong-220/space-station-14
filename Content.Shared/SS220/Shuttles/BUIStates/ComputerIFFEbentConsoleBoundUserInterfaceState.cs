using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ComputerIFFEbentConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public TimeSpan Cooldown;
    public TimeSpan StealthDuration;
}

[Serializable, NetSerializable]
public enum ComputerIFFEbentConsoleUiKey : byte
{
    Key,
}
