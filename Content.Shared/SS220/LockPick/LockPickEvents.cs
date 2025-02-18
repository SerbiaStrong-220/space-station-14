using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.LockPick;

[Serializable]
[NetSerializable]
public sealed partial class LockPickEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
