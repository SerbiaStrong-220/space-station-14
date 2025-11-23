// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.InteractionTeleport;

namespace Content.Shared.SS220.SelfLinkedTeleport;

public abstract class SharedSelfLinkedTeleportSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
    }

    private void OnTeleportTarget(Entity<SelfLinkedTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        WarpTo(ent, args.Target, args.User);

        var ev = new TargetTeleportedEvent(args.Target);
        RaiseLocalEvent(ent, ref ev);
    }

    protected virtual void WarpTo(Entity<SelfLinkedTeleportComponent> ent, EntityUid target, EntityUid user) { }
}
