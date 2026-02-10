// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.AltBlocking;
using Content.Shared.SS220.InstastunResist;

namespace Content.Shared.SS220.InstastunResistOnActiveBlocking;
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
