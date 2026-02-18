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
    }
    public void OnActiveBlock(EntityUid uid, InstastunResistOnActiveBlockingComponent component, ActiveBlockingEvent args)
    {
        if (!TryComp<AltBlockingComponent>(uid, out var BlockComp) || !TryComp<AltBlockingUserComponent>(BlockComp.User, out var userComp))
            return; 

        if (args.Active)
        {
            var resistComp = EnsureComp<InstastunResistComponent>((EntityUid)BlockComp.User);

            resistComp.Active = true;
            resistComp.ResistedStunTypes = component.ResistedStunTypes;

            Dirty((EntityUid)BlockComp.User, resistComp);
            return;
        }

        RemComp<InstastunResistComponent>((EntityUid)BlockComp.User);

        return;
    }
}
