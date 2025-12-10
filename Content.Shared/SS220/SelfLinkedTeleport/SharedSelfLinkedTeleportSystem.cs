// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.SS220.InteractionTeleport;

namespace Content.Shared.SS220.SelfLinkedTeleport;

public abstract class SharedSelfLinkedTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, TeleportUseAttemptEvent>(OnTeleportUseAttempt);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, ExaminedEvent>(OnExamined);
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

    private void OnExamined(Entity<SelfLinkedTeleportComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.LinkedEntity == null)
            args.PushMarkup("linked-teleport-no-exit");
        else
            args.PushMarkup("linked-teleport-has-link");
    }

    protected virtual void Warp(Entity<SelfLinkedTeleportComponent> ent, EntityUid target, EntityUid user) { }

    protected virtual void UpdateVisuals(Entity<SelfLinkedTeleportComponent> ent)
    {
        _appearance.SetData(ent, SelfLinkedVisuals.State, ent.Comp.LinkedEntity != null);

        if (_lights.TryGetLight(ent.Owner, out var light))
            _lights.SetEnabled(ent.Owner, ent.Comp.LinkedEntity != null, light);

        Dirty(ent);
    }
}
