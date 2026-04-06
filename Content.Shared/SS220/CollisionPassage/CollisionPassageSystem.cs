// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.CollisionPassage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.CollisionPassage.Systems;

public sealed class CollisionPassageSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollisionPassageComponent, PreventCollideEvent>(OnPassagePreventCollide);
    }

    private void OnPassagePreventCollide(Entity<CollisionPassageComponent> ent, ref PreventCollideEvent args)
    {
        if (_whitelist.IsWhitelistPass(ent.Comp.Whitelist, args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.AllowIncapacitatedMobs && _mobState.IsIncapacitated(args.OtherEntity))
            args.Cancelled = true;
    }
}
