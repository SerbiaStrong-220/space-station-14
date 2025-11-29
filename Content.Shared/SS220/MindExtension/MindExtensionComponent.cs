using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Mind;

[RegisterComponent]
public sealed partial class MindExtensionComponent : Component
{
    public NetUserId PlayerSession;

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, TrailPointMetaData> Trail = [];

    [ViewVariables(VVAccess.ReadWrite), DataField("riftAccumulator")]
    public TimeSpan? RespawnTimer = default!;

    public float RespawnAccumulatorMax = 1200f;

    public bool IsIC = true;
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
public sealed class UpdateRespawnTime : EntityEventArgs
{
    public TimeSpan Time;

    public UpdateRespawnTime(TimeSpan time)
    {
        Time = time;
    }
}

[Serializable, NetSerializable]
public record struct BodyCont(NetEntity Id, TrailPointMetaData MetaData, BodyStateToEnter State);

[Serializable, NetSerializable]
public enum BodyStateToEnter
{
    Avaible,
    Abandoned,
    Engaged,
    InCryo,
    Destroyed
}

[Serializable, NetSerializable]
public record class TrailPointMetaData
{
    public bool IsAbandoned { get; set; } = false;

    public string EntityName { get; set; } = string.Empty;

    public string EntityDescription { get; set; } = string.Empty;
}
