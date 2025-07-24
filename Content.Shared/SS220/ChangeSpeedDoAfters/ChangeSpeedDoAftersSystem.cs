using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.ChangeSpeedDoAfters;

public sealed class ChangeSpeedDoAftersSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChangeSpeedDoAftersComponent, BeforeDoAfterStartEvent>(OnDoAfterProccess);
        SubscribeLocalEvent<ChangeSpeedDoAftersComponent, DoAfterUpdateEvent>(OnDoAfterUpdate);
    }

    private void OnDoAfterProccess(Entity<ChangeSpeedDoAftersComponent> ent, ref BeforeDoAfterStartEvent args)
    {
        args.Args.Delay *= ent.Comp.Coefficient;

        if (ent.Comp.ChanceToFail == null)
            return;

        if (!_random.Prob(ent.Comp.ChanceToFail.Value))
            return;

        var cancelTime = TimeSpan.FromSeconds(_random.NextFloat(0, (float)args.Args.Delay.TotalSeconds));
        ent.Comp.ScheduledCancelTimes[args.Id] = cancelTime;
    }

    private void OnDoAfterUpdate(Entity<ChangeSpeedDoAftersComponent> ent, ref DoAfterUpdateEvent args)
    {
        if (!ent.Comp.ScheduledCancelTimes.TryGetValue(args.Index, out var cancelTime))
            return;

        var elapsed = _timing.CurTime - args.StartTime;

        if (elapsed < cancelTime)
            return;

        _doAfter.Cancel(ent.Owner, args.Index);
        ent.Comp.ScheduledCancelTimes.Remove(args.Index);

        _popup.PopupEntity(Loc.GetString("trait-nervousness-popup"), ent.Owner, ent.Owner);
    }
}
