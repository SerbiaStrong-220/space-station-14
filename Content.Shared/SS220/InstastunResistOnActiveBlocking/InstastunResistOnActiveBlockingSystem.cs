using Content.Shared.Blocking;
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
        if (!TryComp<BlockingComponent>(uid, out var BlockComp) || !TryComp<BlockingUserComponent>(BlockComp.User, out var userComp)) { return; }
        if (args.Active)
        {
            var resistComp=EnsureComp<InstastunResistComponent>((EntityUid)BlockComp.User);
            resistComp.Active = true;
            return;
        }
        RemComp<InstastunResistComponent>((EntityUid)BlockComp.User);
        return;
    }
}
