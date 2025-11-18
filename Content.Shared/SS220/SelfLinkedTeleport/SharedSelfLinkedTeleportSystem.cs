// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Foldable;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Puppet;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.InteractionTeleport;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.SelfLinkedTeleport;

/// <summary>
/// </summary>
public abstract class SharedSelfLinkedTeleportSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, CanDropTargetEvent>(OnCanDropTarget);
    }

    private void OnCanDropTarget(Entity<SelfLinkedTeleportComponent> ent, ref CanDropTargetEvent args)
    {
        args.CanDrop = true;
        args.Handled = true;
    }
    private void OnTeleportTarget(Entity<SelfLinkedTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        WarpTo(ent, args.Target, args.User);
    }

    protected virtual void WarpTo(Entity<SelfLinkedTeleportComponent> ent, EntityUid target, EntityUid user) { }
}
