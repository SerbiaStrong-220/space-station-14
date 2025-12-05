using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class ExtensionRespawnActionEvent : EntityEventArgs
{
    public NetEntity? Invoker { get; }
    public ExtensionRespawnActionEvent(NetEntity invoker)
    {
        Invoker = invoker;
    }
}
