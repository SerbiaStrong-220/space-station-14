// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Bible;

public abstract class SharedExorcismPerformerSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public enum ExorcismPerformerVisualState : byte
{
    State,
    None,
    Performing,
}

[ByRefEvent]
public record struct ExorcismPerformedEvent(EntityUid Uid, ExorcismPerformerComponent Component, EntityUid Performer)
{
    public EntityUid Uid = Uid;
    public ExorcismPerformerComponent Component = Component;
    public EntityUid Performer = Performer;
}
