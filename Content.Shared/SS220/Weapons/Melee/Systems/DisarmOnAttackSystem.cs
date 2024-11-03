// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Weapons.Melee.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Weapons.Melee.Systems;

public sealed class SharedDisarmOnAttackSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisarmOnAttackComponent, WeaponAttackEvent>(OnAttackEvent);
    }

    private void OnAttackEvent(Entity<DisarmOnAttackComponent> ent, ref WeaponAttackEvent args)
    {
        bool chance;

        switch (args.Type)
        {
            case AttackType.HEAVY:
                chance = _random.Prob(ent.Comp.HeavyAttackChance);
                break;

            case AttackType.LIGHT:
                chance = _random.Prob(ent.Comp.Chance);
                break;

            default:
                chance = false;
                break;
        }

        if (!chance)
            return;

        foreach (var handOrInventoryEntity in _inventory.GetHandOrInventoryEntities(args.Target, SlotFlags.POCKET))
        {
            _handsSystem.TryDrop(args.Target, handOrInventoryEntity);
        }
    }
}
