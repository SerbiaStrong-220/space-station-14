using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Saint.Chaplain.Events.Narsi;

[Serializable, NetSerializable]
public sealed partial class ChaplainNarsiExileDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
