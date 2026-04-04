namespace Content.Shared.SS220.HealOnCollide.Bloodstream;

public abstract partial class SharedBloodstreamExtensionSystem : EntitySystem
{
    public virtual void TryModifyBleedAmount(EntityUid uid, float amount) { }
}
