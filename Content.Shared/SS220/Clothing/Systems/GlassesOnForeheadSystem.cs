// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory;
using Content.Shared.SS220.Clothing.Components;
using Content.Shared.SS220.Clothing.Events;

namespace Content.Shared.SS220.Clothing.Systems;

public sealed class GlassesOnForeheadSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GlassesOnForeheadComponent, GlassesEffectAttemptEvent>(OnEffectAttempt);
    }

    private void OnEffectAttempt(Entity<GlassesOnForeheadComponent> ent, ref GlassesEffectAttemptEvent args)
    {
        if (_inventory.InSlotWithFlags(ent.Owner, SlotFlags.HEAD))
            args.Cancelled = true;
    }
}
