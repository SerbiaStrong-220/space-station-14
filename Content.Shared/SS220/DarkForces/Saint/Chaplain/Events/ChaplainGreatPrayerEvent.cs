using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Saint.Chaplain.Events;

public sealed partial class ChaplainGreatPrayerEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ChaplainGreatPrayerDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
