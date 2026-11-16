using Content.Shared.Shuttles.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class IFFConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public IFFFlags AllowedFlags;
    public IFFFlags Flags;
    public TimeSpan Cooldown; // ss220 spacewar
    public TimeSpan StealthDuration; // ss220 spacewar
}

[Serializable, NetSerializable]
public enum IFFConsoleUiKey : byte
{
    Key,
}
