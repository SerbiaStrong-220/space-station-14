// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Emp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Spawners;

namespace Content.Shared.SS220.Body;

/// <summary>
/// Applies what installed prostheses do to the body: movement speed and the reaction of cybernetic ones to an EMP.
/// </summary>
public sealed partial class ProsthesisSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, RefreshMovementSpeedModifiersEvent>(OnBodyRefreshSpeed);
        SubscribeLocalEvent<BodyComponent, EmpPulseEvent>(OnBodyEmpPulse);

        SubscribeLocalEvent<ProsthesisComponent, OrganGotInsertedEvent>(OnInserted);
        SubscribeLocalEvent<ProsthesisComponent, OrganGotRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<ProsthesisComponent, BodyRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshSpeed);
        SubscribeLocalEvent<ProsthesisComponent, BodyRelayedEvent<EmpPulseEvent>>(OnEmpPulse);
    }

    private void OnBodyRefreshSpeed(Entity<BodyComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        _body.RelayEvent(ent, args);
    }

    private void OnBodyEmpPulse(Entity<BodyComponent> ent, ref EmpPulseEvent args)
    {
        _body.RelayEvent(ent, ref args);
    }

    private void OnInserted(Entity<ProsthesisComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (ent.Comp.SpeedModifier != 1f)
            _movement.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnRemoved(Entity<ProsthesisComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (ent.Comp.SpeedModifier != 1f && !TerminatingOrDeleted(args.Target))
            _movement.RefreshMovementSpeedModifiers(args.Target);
    }

    private void OnRefreshSpeed(Entity<ProsthesisComponent> ent, ref BodyRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        args.Args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    private void OnEmpPulse(Entity<ProsthesisComponent> ent, ref BodyRelayedEvent<EmpPulseEvent> args)
    {
        var body = args.Body.Owner;
        if (ent.Comp.EmpKnockdownTime is { } knockdownTime)
        {
            _stun.TryKnockdown(body, knockdownTime);
            args.Args = args.Args with { Affected = true };
        }

        // the virtual item is spawned and timed on the server, clients get it with the hand state
        if (ent.Comp.EmpHandJamTime is not { } jamTime || _net.IsClient || !TryComp<HandOrganComponent>(ent, out var handOrgan))
            return;

        var hand = handOrgan.HandID;
        _hands.TryDrop(body, hand, checkActionBlocker: false);
        if (!_virtualItem.TrySpawnVirtualItemInHand(ent, body, out var virtualItem, empty: hand, silent: true))
            return;

        // dropping the virtual item would free the hand at once
        EnsureComp<UnremoveableComponent>(virtualItem.Value);
        EnsureComp<TimedDespawnComponent>(virtualItem.Value).Lifetime = (float) jamTime.TotalSeconds;
        args.Args = args.Args with { Affected = true };
    }
}
