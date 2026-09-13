// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.SS220.AltBlocking;
using Content.Shared.SS220.InstastunResist;

namespace Content.Shared.SS220.InstastunResistOnActiveBlocking;
public sealed partial class InstastunResistOnActiveBlockingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InstastunResistOnActiveBlockingComponent, ActiveBlockingStateChanged>(OnActiveBlock);
    }

    public void OnActiveBlock(Entity<InstastunResistOnActiveBlockingComponent> ent, ref ActiveBlockingStateChanged args)
    {
        if (!TryComp<InstastunResistComponent>(ent.Owner, out var resistComp))
            return;

        resistComp.Active = args.State;
    }
}
