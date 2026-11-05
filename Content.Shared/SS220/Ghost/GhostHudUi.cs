// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Ghost;

[Serializable, NetSerializable]
public enum GhostHudUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GhostHudBoundUserInterfaceState(List<GhostHudUiEntry> huds) : BoundUserInterfaceState
{
    public List<GhostHudUiEntry> Huds { get; } = huds;
}

[Serializable, NetSerializable]
public sealed class GhostHudUiEntry(ProtoId<GhostHudPrototype> id, LocId name, bool enabled)
{
    public ProtoId<GhostHudPrototype> Id { get; } = id;
    public LocId Name { get; } = name;
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public sealed class GhostHudToggledMessage(ProtoId<GhostHudPrototype> hud, bool enabled) : BoundUserInterfaceMessage
{
    public ProtoId<GhostHudPrototype> Hud { get; } = hud;
    public bool Enabled { get; } = enabled;
}
