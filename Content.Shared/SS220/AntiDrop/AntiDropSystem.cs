using Content.Shared.Standing;

namespace Content.Shared.SS220.AntiDrop;

public sealed class AntiDropSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AntiDropComponent, FellDownThrowAttemptEvent>(OnFellDownAttempt);
    }

    private void OnFellDownAttempt(Entity<AntiDropComponent> ent, ref FellDownThrowAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
