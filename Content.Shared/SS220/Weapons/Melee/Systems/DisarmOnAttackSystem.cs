// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Weapons.Melee.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Weapons.Melee..Systems;

public sealed class SharedDisarmOnAttackSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisarmOnAttackComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DisarmOnAttackComponent, WeaponAttackEvent>(OnAttackEvent);
    }

    private void OnAttackEvent(Entity<DisarmOnAttackComponent> ent, ref ComponentInit> args)
    {
        if(ent.Comp.HeavyAttackChance is null)
            ent.Comp.HeavyAttackChance = ent.Comp.Chance;
    }

    private void OnAttackEvent(Entity<DisarmOnAttackComponent> ent, ref WeaponAttackEvent args)
    {
        switch (args.Type)
        {
            case AttackType.HEAVY:
                if (ent.Comp.DisarmOnHeavyAtack)
                    DisarmOnHeavyAtack(ent, args.Target);
                break;
            case AttackType.LIGHT:
                if (entity.Comp.DisarmOnLightAtack)
                    DisarmOnAtack(ent, args.Target);
                break;
        }
    }

    private void DisarmOnAtack(Entity<DisarmOnAttackComponent> ent, EntityUid target)
    {
        foreach (var handOrInventoryEntity in _inventory.GetHandOrInventoryEntities(target, SlotFlags.POCKET))
        {
            if (!_random.Prob(ent.Comp.Chance))
                continue;
            _handsSystem.TryDisarm(target, handOrInventoryEntity);
        }
    }
}
