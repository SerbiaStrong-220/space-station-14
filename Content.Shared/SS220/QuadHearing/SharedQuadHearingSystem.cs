using Robust.Shared.Audio.Components;

namespace Content.Shared.SS220.QuadHearing;

public abstract class SharedQuadHearingSystem : EntitySystem
{
    public void RegisterTarget(Entity<AudioComponent> entity)
    {
        EnsureComp<QuadHearingTargetComponent>(entity);
        Dirty(entity);
    }
}
