using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Ui;

[Serializable, NetSerializable]
public sealed class BodyAnalyzerTargetUpdate(NetEntity target) : BoundUserInterfaceState
{
    public NetEntity Target { get; } = target;
}
