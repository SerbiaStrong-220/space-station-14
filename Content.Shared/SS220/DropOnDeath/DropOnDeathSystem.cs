// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.DropOnDeath;

/// <summary>
/// Forcibly unequips items marked to drop when their wearer dies.
/// </summary>
public sealed partial class DropOnDeathSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        // Removing clothing can also unequip dependent slots, so iterate over a snapshot.
        foreach (var item in _inventory.GetHandOrInventoryEntities(args.Target).ToArray())
        {
            if (!HasComp<DropOnDeathComponent>(item))
                continue;

            if (!_containers.TryGetContainingContainer((item, null, null), out var container))
                continue;

            if (container.Owner != args.Target)
                continue;

            if (_inventory.HasSlot(args.Target, container.ID))
            {
                // Use the inventory API to handle dependent slots and attached helmets.
                _inventory.TryUnequip(args.Target, container.ID, silent: true, force: true, triggerHandContact: true);
                continue;
            }

            if (!_hands.IsHolding(args.Target, item))
                continue;

            _containers.Remove(item, container, force: true);
        }
    }
}
