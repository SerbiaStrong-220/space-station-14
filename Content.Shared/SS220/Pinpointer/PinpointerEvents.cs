// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pinpointer;

[Serializable, NetSerializable]
public enum PinpointerUIKey
{
    Key
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerTargetPick(NetEntity target) : BoundUserInterfaceMessage//ToDo_SS220 fix cursed pinpointer
{
    public NetEntity Target = target;
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerCrewTargetPick(NetEntity target) : BoundUserInterfaceMessage//ToDo_SS220 fix cursed pinpointer
{
    public NetEntity Target = target;
}


[Serializable]
[NetSerializable]
public sealed partial class PinpointerDnaPick(string? dna) : BoundUserInterfaceMessage//ToDo_SS220 fix cursed pinpointer
{
    public string? Dna = dna;
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerComponentUIState(HashSet<TrackedItem> targets) : BoundUserInterfaceState//ToDo_SS220 fix cursed pinpointer
{
    public HashSet<TrackedItem> Targets = targets;
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerCrewUIState(HashSet<TrackedItem> sensors) : BoundUserInterfaceState//ToDo_SS220 fix cursed pinpointer
{
    public HashSet<TrackedItem> Sensors = sensors;
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerItemUIState(HashSet<TrackedItem> items) : BoundUserInterfaceState//ToDo_SS220 fix cursed pinpointer
{
    public HashSet<TrackedItem> Items = items;
}

[Serializable]
[NetSerializable]
public struct TrackedItem(NetEntity entity, string name)//ToDo_SS220 fix cursed pinpointer
{
    public NetEntity Entity { get; } = entity;
    public string Name { get; } = name;
}

public enum PinpointerMode//ToDo_SS220 fix cursed pinpointer
{
    Crew,
    Item,
    Component
}
