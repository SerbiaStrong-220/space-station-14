// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.SS220.Grab;

public sealed partial class GrabShoveKnockoutSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrabShoveKnockoutComponent, GrabStageChangeEvent>(OnGrabStageChange);
        SubscribeLocalEvent<GrabShoveKnockoutComponent, DisarmAttackPerformedEvent>(OnDisarm);
    }

    private void OnGrabStageChange(Entity<GrabShoveKnockoutComponent> ent, ref GrabStageChangeEvent args)
    {
        if (args.Grabber != ent.Owner)
            return;

        if (args.NewStage == GrabStage.None)
            RemComp<GrabShoveKnockoutComponent>(ent);
    }

    private void OnDisarm(Entity<GrabShoveKnockoutComponent> ent, ref DisarmAttackPerformedEvent args)
    {
        if (args.Target == null)
            return;

        if (!TryComp<GrabberComponent>(ent, out var grabber))
            return;

        if (grabber.Grabbing != args.Target)
            return;

        _stun.TryKnockdown(args.Target.Value, ent.Comp.KnockdownTime);
        RemComp<GrabShoveKnockoutComponent>(ent);
    }

    public void SetupKnockout(EntityUid uid, TimeSpan knockdownTime)
    {
        var knockout = EnsureComp<GrabShoveKnockoutComponent>(uid);
        knockout.KnockdownTime = knockdownTime;
    }
}
