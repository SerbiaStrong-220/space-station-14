using Content.Shared.Climbing.Events;
using Content.Shared.SS220.Felinid.Components;

namespace Content.Shared.SS220.Felinid;

public sealed class FelinidClimbSpeedSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<FelinidClimbSpeedComponent, ModifyClimbDelayEvent>(OnModifyClimbDelay);
    }

    private void OnModifyClimbDelay(Entity<FelinidClimbSpeedComponent> ent, ref ModifyClimbDelayEvent args)
    {
        if (args.User == ent.Owner)
            args.ModifyDelay(ent.Comp.DelayMultiplier);
    }
}
