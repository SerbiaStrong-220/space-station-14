using Robust.Shared.Random;

namespace Content.Shared.SS220.Grab;

public sealed partial class GrabSystem
{
    private void InitializeResistance()
    {
        SubscribeLocalEvent<GrabResistanceComponent, GrabBreakoutAttemptAlertEvent>(OnBreakoutAttemptAlert);
    }

    private void OnBreakoutAttemptAlert(Entity<GrabResistanceComponent> ent, ref GrabBreakoutAttemptAlertEvent args)
    {
        if (!TryComp<GrabbableComponent>(ent, out var grabbable))
            return;

        if (grabbable.GrabStage == GrabStage.None)
            return;

        // alerts system doesn't handles cooldowns
        var (_, cooldownEnd) = GetResistanceCooldown((ent.Owner, ent.Comp));
        if (cooldownEnd > _timing.CurTime)
            return;

        args.Handled = true;

        TryBreakGrab((ent.Owner, grabbable));
    }

    /// <summary>
    /// Gets cooldown resistance for grabbable
    /// </summary>
    /// <returns>(TimeStart, TimeEnd)</returns>
    private (TimeSpan, TimeSpan) GetResistanceCooldown(Entity<GrabResistanceComponent?> grabbable)
    {
        if (!Resolve(grabbable, ref grabbable.Comp, false))
            return (TimeSpan.Zero, TimeSpan.Zero);

        var resistance = grabbable.Comp;

        var start = resistance.LastBreakoutAttemptAt;
        var end = start + resistance.BreakoutAttemptCooldown;

        return (start, end);
    }

    public void TryBreakGrab(Entity<GrabbableComponent?> grabbable)
    {
        if (!Resolve(grabbable, ref grabbable.Comp))
            return;

        if (!TryComp<GrabResistanceComponent>(grabbable, out var resistance)) // you don't have ability to resist lol
            return;

        var (_, cooldownEnd) = GetResistanceCooldown((grabbable.Owner, resistance));
        if (cooldownEnd > _timing.CurTime)
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
            _popup.PopupPredicted(Loc.GetString(resistance.ResistingPopup, ("grabbable", MetaData(grabbable).EntityName)), grabbable, grabbable);
            resistance.LastBreakoutAttemptAt = _timing.CurTime;
            UpdateAlertsGrabbable((grabbable, grabbable.Comp), grabbable.Comp.GrabStage);
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
