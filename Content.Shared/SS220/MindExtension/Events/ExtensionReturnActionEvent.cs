using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class ExtensionReturnActionEvent : EntityEventArgs
{
    public NetEntity? Target { get; }
    public ExtensionReturnActionEvent(NetEntity target)
    {
        Target = target;
    }
}
