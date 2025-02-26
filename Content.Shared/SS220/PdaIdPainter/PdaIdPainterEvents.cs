using Robust.Shared.Serialization;

namespace Content.Shared.SS220.PdaIdPainter;

[Serializable, NetSerializable]
public enum PdaIdPainterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PdaIdPainterPickedPdaMessage : BoundUserInterfaceMessage
{
    public string Proto;

    public PdaIdPainterPickedPdaMessage(string proto)
    {
        Proto = proto;
    }
}

[Serializable, NetSerializable]
public sealed class PdaIdPainterPickedIdMessage : BoundUserInterfaceMessage
{
    public string Proto;

    public PdaIdPainterPickedIdMessage(string proto)
    {
        Proto = proto;
    }
}

[Serializable, NetSerializable]
public sealed class PdaIdPainterOpenWindowMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class PdaIdPainterChangeStateMessage : EntityEventArgs
{
    public bool IsActive;
    public NetEntity Target;

    public PdaIdPainterChangeStateMessage(bool isActive, NetEntity target)
    {
        IsActive = isActive;
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class PdaIdPainterBoundState : BoundUserInterfaceState
{
    public NetEntity? TargetId;
    public NetEntity? TargetPda;

    public PdaIdPainterBoundState(NetEntity? targetId, NetEntity? targetPda)
    {
        TargetId = targetId;
        TargetPda = targetPda;
    }
}
