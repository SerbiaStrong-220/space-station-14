using Robust.Shared.Audio.Components;

namespace Content.Shared.SS220.FourChannelHearing;

public abstract class SharedFourChannelHearingSystem : EntitySystem
{

    public void RegisterTarget(Entity<AudioComponent> entity)
    {
        EnsureComp<FourChannelHearingTargetComponent>(entity);
        Dirty(entity);
    }
}
