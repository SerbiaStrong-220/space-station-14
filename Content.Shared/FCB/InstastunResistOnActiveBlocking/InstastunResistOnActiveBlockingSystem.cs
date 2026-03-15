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

    public void OnActiveBlock(Entity<InstastunResistOnActiveBlockingComponent> ent, ref ActiveBlockingEvent args)
    {
        if (!TryComp<InstastunResistComponent>(ent.Owner, out var resistComp))
            return;

        resistComp.Active = args.Active;
    }
}
