// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Popups;
using Content.Shared.SS220.InteractionTeleport;

namespace Content.Shared.SS220.SelfLinkedTeleport;

public abstract class SharedSelfLinkedTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, TeleportUseAttemptEvent>(OnTeleportUseAttempt);
    }

    private void OnTeleportTarget(Entity<SelfLinkedTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        Warp(ent, args.Target, args.User);

        var ev = new TargetTeleportedEvent(args.Target);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnTeleportUseAttempt(Entity<SelfLinkedTeleportComponent> ent, ref TeleportUseAttemptEvent args)
    {
        if (ent.Comp.LinkedEntity == null)
        {
            _popup.PopupPredicted(Loc.GetString("linked-teleport-no-exit"), ent, args.User, PopupType.MediumCaution);
            args.Cancelled = true;
        }
    }

    protected virtual void Warp(Entity<SelfLinkedTeleportComponent> ent, EntityUid target, EntityUid user) { }
}
