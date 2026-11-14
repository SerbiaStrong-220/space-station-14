using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Ui;

[Serializable, NetSerializable]
public sealed class BodyAnalyzerTargetUpdate(NetEntity? target, bool tableNotLinked = false) : BoundUserInterfaceState
{
    public NetEntity? Target { get; } = target;
    public bool TableNotLinked { get; } = tableNotLinked;
}
