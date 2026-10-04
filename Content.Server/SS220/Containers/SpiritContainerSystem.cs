// SS220 Spirits can contain begin
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Ghost;
using Content.Shared.Movement.Events;
using Content.Shared.Revenant.Components;
using Content.Shared.SS220.Containers;
using Content.Shared.SS220.Containers.Components;
using Content.Shared.SS220.DarkReaper;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Containers;

public sealed class SpiritContainerSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityStorageSystem _storage = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiritContainerComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<SpiritContainerComponent, SpiritEscapeDoAfterEvent>(OnEscape);
        SubscribeLocalEvent<SpiritContainerComponent, DoAfterAttemptEvent<SpiritEscapeDoAfterEvent>>(OnEscapeAttempt);
        SubscribeLocalEvent<SpiritContainerComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnRelayMovement(Entity<SpiritContainerComponent> ent, ref ContainerRelayMovementEntityEvent args)
    {
        if (!CanEscape(ent, args.Entity))
            return;

        if (ent.Comp.Escaping.ContainsKey(args.Entity))
            return;

        EnsureComp<DoAfterComponent>(args.Entity);
        var doAfter = new DoAfterArgs(
            EntityManager,
            args.Entity,
            TimeSpan.FromSeconds(ent.Comp.EscapeTime),
            new SpiritEscapeDoAfterEvent(),
            ent.Owner,
            target: ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = false,
            DistanceThreshold = null,
            NeedHand = false,
            RequireCanInteract = false,
            BlockDuplicate = true,
            CancelDuplicate = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (!_doAfter.TryStartDoAfter(doAfter, out var id))
            return;

        if (_doAfter.IsRunning(id))
            ent.Comp.Escaping[args.Entity] = id.Value;

        _popup.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), args.Entity, args.Entity);
    }

    private void OnEscapeAttempt(Entity<SpiritContainerComponent> ent, ref DoAfterAttemptEvent<SpiritEscapeDoAfterEvent> args)
    {
        if (!CanEscape(ent, args.DoAfter.Args.User))
            args.Cancel();
    }

    private void OnRemoved(Entity<SpiritContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (ent.Comp.Escaping.Remove(args.Entity, out var id))
            _doAfter.Cancel(id);
    }

    private bool CanEscape(Entity<SpiritContainerComponent> ent, EntityUid user)
    {
        return !HasComp<GhostComponent>(user) &&
               (HasComp<RevenantComponent>(user) || HasComp<DarkReaperComponent>(user)) &&
               _actionBlocker.CanMove(user) &&
               TryComp<EntityStorageComponent>(ent, out var storage) &&
               !storage.Open &&
               storage.Contents.Contains(user);
    }

    private void OnEscape(Entity<SpiritContainerComponent> ent, ref SpiritEscapeDoAfterEvent args)
    {
        ent.Comp.Escaping.Remove(args.User);

        if (args.Cancelled || args.Handled || !CanEscape(ent, args.User))
            return;

        if (_storage.Remove(args.User, ent.Owner))
            args.Handled = true;
    }
}
// SS220 Spirits can contain end
