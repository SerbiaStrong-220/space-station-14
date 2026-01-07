using Robust.Shared.Random;

namespace Content.Shared.SS220.Grab;

public sealed partial class GrabSystem
{
    public void TryBreakGrab(Entity<GrabbableComponent?> grabbable)
    {
        if (!Resolve(grabbable, ref grabbable.Comp))
            return;

        if (!TryComp<GrabResistanceComponent>(grabbable, out var resistance)) // you don't have ability to resist lol
            return;

        if (resistance.LastBreakoutAttemptAt + resistance.BreakoutAttemptCooldown >= _timing.CurTime)
            return;

        var chance = resistance.CurrentStageBreakoutChance[grabbable.Comp.GrabStage];

        if (chance <= 0)
            return;

        if (chance >= 1 || _random.Prob(chance))
        {
            BreakGrab(grabbable);
        }
        else
        {
            resistance.LastBreakoutAttemptAt = _timing.CurTime;
        }
    }

    public void RefreshGrabResistance(Entity<GrabbableComponent> grabbable)
    {
        if (!TryComp<GrabResistanceComponent>(grabbable, out var resistance))
            return;

        var ev = new GrabResistanceModifiersEvent(grabbable, resistance.BaseStageBreakoutChance);
        RaiseLocalEvent(grabbable, ev);

        resistance.CurrentStageBreakoutChance = ev.CurrentStageBreakoutChance;
    }
}
