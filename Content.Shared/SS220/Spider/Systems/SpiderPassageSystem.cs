// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Spider;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.Spider;

public sealed class SpiderPassageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderPassageComponent, PreventCollideEvent>(OnPassagePreventCollide);
    }

    private void OnPassagePreventCollide(Entity<SpiderPassageComponent> ent, ref PreventCollideEvent args)
    {
        if (HasComp<SpiderComponent>(args.OtherEntity))
            args.Cancelled = true;
    }
}
