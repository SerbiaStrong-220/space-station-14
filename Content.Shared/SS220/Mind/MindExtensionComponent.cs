using Content.Shared.Ghost;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Mind;

[RegisterComponent]
public sealed partial class MindExtensionComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> Trail = [];
}


[Serializable, NetSerializable]
public sealed class ExtensionRespawnActionEvent : EntityEventArgs
{
    public NetEntity? Invoker { get; }
    public ExtensionRespawnActionEvent(NetEntity invoker)
    {
        Invoker = invoker;
    }
}

[Serializable, NetSerializable]
public sealed class ExtensionReturnActionEvent : EntityEventArgs
{
    public NetEntity? Target { get; }
    public ExtensionReturnActionEvent(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class GetTrailEvent : EntityEventArgs
{
    public HashSet<NetEntity> Trail { get; }
    public GetTrailEvent(HashSet<NetEntity> trail)
    {
        Trail = trail;
    }
}

[Serializable, NetSerializable]
public sealed class GhostBodyListRequestEvent : EntityEventArgs { }

[Serializable, NetSerializable]
public sealed class GhostBodyListResponseEvent : EntityEventArgs
{
    public List<BodyCont> Bodies { get; }
    public GhostBodyListResponseEvent(List<BodyCont> bodies)
    {
        Bodies = bodies;
    }
}

[Serializable, NetSerializable]
public record struct BodyCont(NetEntity Id, string Name, bool IsAvailble);
