using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ChangeSpeedDoAfters.Events;

/// <summary>
/// This event raised every frameTime on user
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DoAfterUpdateEvent : EntityEventArgs
{
    public TimeSpan StartTime;
    public ushort Index;

    public DoAfterUpdateEvent(TimeSpan startTime, ushort index)
    {
        StartTime = startTime;
        Index = index;
    }
}

/// <summary>
/// This event raised only on start DoAfter
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BeforeDoAfterStartEvent : EntityEventArgs
{
    public DoAfterArgs Args;
    public ushort Id;

    public BeforeDoAfterStartEvent(DoAfterArgs args, ushort id)
    {
        Args = args;
        Id = id;
    }
}
