// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;

namespace Content.Shared.SS220.InstastunResist;
public sealed partial class InstastunResistSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InstastunResistComponent, StunAttemptEvent>(OnStunAttempt);
        SubscribeLocalEvent<InventoryComponent, StunAttemptEvent>(RelayInventoryEvent);
        SubscribeLocalEvent<WearableInstastunResistComponent, InventoryRelayedEvent<StunAttemptEvent>>(RelayedClothingEvent);
        SubscribeLocalEvent<HandsComponent, StunAttemptEvent>(RelayToHeld);
    }

    public void OnStunAttempt(Entity<InstastunResistComponent> ent, ref StunAttemptEvent args)
    {
        if (ent.Comp.ResistedStunTypes.Contains(args.Origin))
            args.StunCancelled = true;
    }

    public void RelayedClothingEvent(Entity<WearableInstastunResistComponent> ent, ref InventoryRelayedEvent<StunAttemptEvent> args)
    {
        if (ent.Comp.ResistedStunTypes.Contains(args.Args.Origin))
            args.Args.StunCancelled = true;
    }

    public void RelayInventoryEvent(Entity<InventoryComponent> ent, ref StunAttemptEvent args)
    {
        _inventory.RelayEvent(ent, ref args);
    }

    public void RelayToHeld(Entity<HandsComponent> ent, ref StunAttemptEvent args)
    {
        foreach (var hand in ent.Comp.Hands)
        {
            if (_hands.TryGetHeldItem(ent.Owner, hand.Key, out var item))
            {
                if (TryComp<InstastunResistComponent>(item, out var resistComp) && resistComp.Active)
                {
                    if (resistComp.ResistedStunTypes.Contains(args.Origin))
                        args.StunCancelled = true;
                }
            }
        }
    }
}

[ByRefEvent]
public record struct StunAttemptEvent(StunSource Origin, bool StunCancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
}

public enum StunSource : byte
{
    Creampie = 0,
    Projectile = 1 //Works for StunOnCollide projectiles
}


