// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Robust.Shared.Utility;
using static Content.Shared.Inventory.InventorySystem;

namespace Content.Shared.FCB.InstastunResist;
public sealed partial class InstastunResistSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InstastunResistComponent, StunAttemptEvent>(OnStunAttempt);
        SubscribeLocalEvent<InventoryComponent, StunAttemptEvent>(RelayInventoryEvent);
    }

    public void OnStunAttempt(Entity<InstastunResistComponent> ent, ref StunAttemptEvent args)
    {
        if (ent.Comp.ResistedStunTypes.Contains(args.Origin))
            args.StunCancelled = true;
    }

    public void RelayInventoryEvent(Entity<InventoryComponent> ent, ref StunAttemptEvent args)
    {
        _inventory.RelayEvent<StunAttemptEvent>(ent, ref args);
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


