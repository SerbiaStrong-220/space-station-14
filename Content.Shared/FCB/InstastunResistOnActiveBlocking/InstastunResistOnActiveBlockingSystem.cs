// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.FCB.AltBlocking;
using Content.Shared.FCB.InstastunResist;

namespace Content.Shared.FCB.InstastunResistOnActiveBlocking;
public sealed partial class InstastunResistOnActiveBlockingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstastunResistOnActiveBlockingComponent, ActiveBlockingEvent>(OnActiveBlock); 
        SubscribeLocalEvent<InstastunResistOnActiveBlockingComponent, StunAttemptEvent>(OnStunAttempt);
    }

    public void OnStunAttempt(Entity<InstastunResistOnActiveBlockingComponent> ent, ref StunAttemptEvent args)
    {
        if (args.StunCancelled)
            return;

        if (ent.Comp.Active && ent.Comp.ResistedStunTypes.Contains(args.Origin))
            args.StunCancelled = true;
    }

    public void OnActiveBlock(Entity<InstastunResistOnActiveBlockingComponent> ent, ref ActiveBlockingEvent args)
    {
        if (!TryComp<AltBlockingComponent>(ent.Owner, out var blockComp) || !TryComp<AltBlockingUserComponent>(blockComp.User, out var userComp))
            return;

        ent.Comp.Active = args.Active;
        return;

        if (args.Active)
        {

            var resistComp = EnsureComp<InstastunResistComponent>((EntityUid)blockComp.User);

            resistComp.Active = true;
            resistComp.ResistedStunTypes = ent.Comp.ResistedStunTypes;

            Dirty((EntityUid)blockComp.User, resistComp);
            return;
        }

        RemComp<InstastunResistComponent>((EntityUid)blockComp.User);

        return;
    }
}
