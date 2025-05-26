using Robust.Shared.Serialization;

namespace Content.Shared.SS220.LocateAi;

public abstract class SharedLocateAiSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed class LocateAiEvent : EntityEventArgs
{
    public NetEntity Tool;
    public bool IsNear;

    public LocateAiEvent(NetEntity tool, bool isNear)
    {
        Tool = tool;
        IsNear = isNear;
    }
}
