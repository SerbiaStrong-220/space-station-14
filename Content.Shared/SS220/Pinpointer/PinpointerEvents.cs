// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pinpointer;

[Serializable, NetSerializable]
public enum PinpointerUIKey
{
    Key,
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerTargetPick : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public PinpointerTargetPick(NetEntity target)
    {
        Target = target;
    }
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerDnaPick : BoundUserInterfaceMessage
{
    public string? Dna;

    public PinpointerDnaPick(string? dna)
    {
        Dna = dna;
    }
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerCrewUIState : BoundUserInterfaceState
{
    public HashSet<TrackedItem> Sensors;

    public PinpointerCrewUIState(HashSet<TrackedItem> sensors)
    {
        Sensors = sensors;
    }
}

[Serializable]
[NetSerializable]
public sealed partial class PinpointerItemUIState : BoundUserInterfaceState
{
    public HashSet<TrackedItem> Items;

    public PinpointerItemUIState(HashSet<TrackedItem> items)
    {
        Items = items;
    }
}

[Serializable]
[NetSerializable]
public struct TrackedItem
{
    public NetEntity Entity { get; }
    public string Name { get; }

    public TrackedItem(NetEntity entity, string name)
    {
        Entity = entity;
        Name = name;
    }
}

public enum PinpointerMode
{
    Crew,
    Item,
}
