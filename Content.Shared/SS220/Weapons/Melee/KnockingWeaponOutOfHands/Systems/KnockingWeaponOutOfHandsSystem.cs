// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Weapons.Melee.KnockingWeaponOutOfHands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Weapons.Melee.KnockingWeaponOutOfHands.Systems;

public sealed class KnockingWeaponOutOfHandsSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KnockingWeaponOutOfHandsComponent, WeaponAttackEvent>(OnAttackEvent);
    }

    private void OnAttackEvent(Entity<KnockingWeaponOutOfHandsComponent> entity, ref WeaponAttackEvent args)
    {
        switch(args.Type)
        {
            case AttackType.HEAVY:
                if(entity.Comp.DropOnHeavyAtack)
                    DropOnAtack(entity, args.Target);
                break;
            case AttackType.LIGHT:
                if(entity.Comp.DropOnLightAtack)
                    DropOnAtack(entity, args.Target);
                break;
        }
    }

    private void DropOnAtack(Entity<KnockingWeaponOutOfHandsComponent> entity, EntityUid target)
    {
        foreach (var handOrInventoryEntity in _inventory.GetHandOrInventoryEntities(target, SlotFlags.POCKET))
        {
            if (!HasComp<MeleeWeaponComponent>(handOrInventoryEntity)
                || !HasComp<GunComponent>(handOrInventoryEntity))
                continue;
            if (!_random.Prob(entity.Comp.Chance))
                continue;
            _handsSystem.TryDrop(target, handOrInventoryEntity);
        }
    }
}
