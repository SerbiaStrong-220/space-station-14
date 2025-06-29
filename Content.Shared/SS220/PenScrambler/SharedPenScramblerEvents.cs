using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.PenScrambler;

[Serializable]
[NetSerializable]
public sealed partial class CopyDnaToPenEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable]
[NetSerializable]
public sealed partial class CopyDnaFromPenToImplantEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
