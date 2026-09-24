// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Network;
namespace Content.Shared.SS220.Grab;

public partial class SharedGrabSystem
{
    private void InitializeResistance()
    {
        SubscribeLocalEvent<GrabResistanceComponent, GrabBreakoutAttemptAlertEvent>(OnBreakoutAttemptAlert);
        SubscribeLocalEvent<GrabResistanceComponent, GrabStageChangeEvent>(OnGrabStageChange);
    }

    private void OnGrabStageChange(Entity<GrabResistanceComponent> ent, ref GrabStageChangeEvent args)
    {
        if (args.Grabbable != ent.Owner)
            return;

        if (args.OldStage == GrabStage.None && args.NewStage > GrabStage.None)
        {
            ent.Comp.LastBreakoutAttemptAt = _timing.CurTime;
            ent.Comp.NextBreakoutAttemptAt = _timing.CurTime + ent.Comp.FirstBreakoutAttemptDelay;
        }
    }

    private void OnBreakoutAttemptAlert(Entity<GrabResistanceComponent> ent, ref GrabBreakoutAttemptAlertEvent args)
    {
        if (!TryComp<GrabbableComponent>(ent, out var grabbable))
            return;

        if (grabbable.GrabStage == GrabStage.None)
            return;

        // alerts system doesn't handles cooldowns
        var cooldown = GetResistanceCooldownSpan((ent, ent.Comp));
        if (cooldown > TimeSpan.Zero)
            return;

        args.Handled = true;

        TryBreakGrab((ent.Owner, grabbable));
    }

    private TimeSpan GetResistanceCooldownSpan(Entity<GrabResistanceComponent?> grabbable)
    {
        if (!Resolve(grabbable, ref grabbable.Comp, false))
            return TimeSpan.Zero;

        var cooldown = grabbable.Comp.NextBreakoutAttemptAt - _timing.CurTime;
        return cooldown > TimeSpan.Zero ? cooldown : TimeSpan.Zero;
    }

    /// <summary>
    /// Gets cooldown resistance for grabbable
    /// </summary>
    /// <returns>(TimeStart, TimeEnd)</returns>
    private (TimeSpan, TimeSpan) GetResistanceStartEndTime(Entity<GrabResistanceComponent?> grabbable)
    {
        if (!Resolve(grabbable, ref grabbable.Comp, false))
            return (TimeSpan.Zero, TimeSpan.Zero);

        var resistance = grabbable.Comp;

        var start = resistance.LastBreakoutAttemptAt;
        var end = resistance.NextBreakoutAttemptAt;

        return (start, end);
    }

    public void TryBreakGrab(Entity<GrabbableComponent?> grabbable)
    {
        if (!Resolve(grabbable, ref grabbable.Comp))
            return;

        if (!TryComp<GrabResistanceComponent>(grabbable, out var resistance)) // you don't have ability to resist lol
            return;

        var (_, cooldownEnd) = GetResistanceStartEndTime((grabbable.Owner, resistance));
        if (cooldownEnd > _timing.CurTime)
            return;

        if (!resistance.CurrentStageBreakoutChance.TryGetValue(grabbable.Comp.GrabStage, out var chance))
        {
            RefreshGrabResistance(grabbable!);
            return;
        }

        if (chance <= 0)
            return;

        // TODO: Once we have predicted randomness delete this for something sane...
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(grabbable.Owner).Id);
        var rand = new System.Random(seed);

        if (chance >= 1 || rand.Prob(chance))
        {
            BreakGrab(grabbable);
        }
        else
        {
            _popup.PopupPredicted(Loc.GetString(resistance.ResistingPopup, ("grabbable", MetaData(grabbable).EntityName)), grabbable, grabbable);
            resistance.LastBreakoutAttemptAt = _timing.CurTime;
            resistance.NextBreakoutAttemptAt = _timing.CurTime + resistance.BreakoutAttemptCooldown;
            UpdateAlertFor(grabbable, grabbable.Comp.Alert, grabbable.Comp.GrabStage, true);
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
