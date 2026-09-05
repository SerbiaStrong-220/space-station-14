// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.Teleport.Systems;
using Robust.Shared.Map;

namespace Content.Shared.SS220.AutoLinkingTeleport;

public abstract partial class SharedAutoLinkingTeleportSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private SharedTeleportSystem _teleport = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoLinkingTeleportComponent, TeleportRequestEvent>(OnTeleportRequest);
        SubscribeLocalEvent<AutoLinkingTeleportComponent, GhostTeleportRequestEvent>(OnGhostTeleportRequest);
        SubscribeLocalEvent<AutoLinkingTeleportComponent, TeleportUseAttemptEvent>(OnTeleportUseAttempt);
        SubscribeLocalEvent<AutoLinkingTeleportComponent, ExaminedEvent>(OnExamined);
    }

    private void OnTeleportRequest(Entity<AutoLinkingTeleportComponent> ent, ref TeleportRequestEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetDestination(ent, out var destination))
            return;

        var destinationCoordinates = Transform(destination).Coordinates;

        if (!TryTeleport(ent, args.Target, args.User, destinationCoordinates))
            return;

        args.Handled = true;
    }

    private void OnGhostTeleportRequest(Entity<AutoLinkingTeleportComponent> ent, ref GhostTeleportRequestEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetDestination(ent, out var destination))
            return;

        var destinationCoordinates = Transform(destination).Coordinates;
        if (!_teleport.TryTeleportGhost(args.Target, destinationCoordinates))
            return;

        args.Handled = true;
    }

    private void OnTeleportUseAttempt(Entity<AutoLinkingTeleportComponent> ent, ref TeleportUseAttemptEvent args)
    {
        if (ent.Comp.LinkedTeleporter != null)
            return;

        _popup.PopupPredicted(
            Loc.GetString("auto-linking-teleport-no-destination"),
            null,
            ent,
            args.User,
            PopupType.MediumCaution);

        args.Cancelled = true;
    }

    private void OnExamined(Entity<AutoLinkingTeleportComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.LinkedTeleporter != null)
            args.PushMarkup(Loc.GetString("auto-linking-teleport-has-destination"));
        else
            args.PushMarkup(Loc.GetString("auto-linking-teleport-no-destination"));
    }

    private bool TryGetDestination(Entity<AutoLinkingTeleportComponent> ent, out EntityUid destination)
    {
        destination = ent.Comp.LinkedTeleporter ?? EntityUid.Invalid;

        if (!Exists(destination))
            return false;

        if (TerminatingOrDeleted(destination))
            return false;

        return true;
    }

    protected virtual bool TryTeleport(
        Entity<AutoLinkingTeleportComponent> ent,
        EntityUid target,
        EntityUid user,
        EntityCoordinates destinationCoordinates)
    {
        return _teleport.TryTeleport(ent, target, destinationCoordinates);
    }

    protected virtual void UpdateVisuals(Entity<AutoLinkingTeleportComponent> ent)
    {
        _appearance.SetData(ent, AutoLinkingTeleportVisuals.Linked, ent.Comp.LinkedTeleporter != null);

        if (_lights.TryGetLight(ent.Owner, out var light))
            _lights.SetEnabled(ent.Owner, ent.Comp.LinkedTeleporter != null, light);

        Dirty(ent);
    }
}
