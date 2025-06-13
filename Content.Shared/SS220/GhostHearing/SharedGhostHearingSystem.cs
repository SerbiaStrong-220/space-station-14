using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.GhostHearing;

public abstract class SharedGhostHearingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
}

public interface IHearableChannelPrototype : IPrototype
{
    string ID { get; }
    string LocalizedName { get; }
    Color Color { get; }
}
