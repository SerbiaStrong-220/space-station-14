using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Saint.Chaplain.Events;

public sealed partial class ChaplainStartExorcismEvent : EntityTargetActionEvent
{

}

public sealed partial class ChaplainExorcismEvent : EntityEventArgs
{

}

[Serializable, NetSerializable]
public sealed partial class ChaplainExorcismDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
