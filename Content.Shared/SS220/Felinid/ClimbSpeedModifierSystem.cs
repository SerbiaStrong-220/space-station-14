using Content.Shared.Climbing.Events;
using Content.Shared.SS220.Felinid.Components;

namespace Content.Shared.SS220.Felinid;

public sealed class ClimbSpeedModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ClimbSpeedModifierComponent, GetClimbDelayModifierEvent>(OnModifyClimbDelay);
    }

    private void OnModifyClimbDelay(Entity<ClimbSpeedModifierComponent> ent, ref GetClimbDelayModifierEvent args)
    {
        if (args.User == ent.Owner)
            args.ModifyDelay(ent.Comp.DelayMultiplier);
    }
}
