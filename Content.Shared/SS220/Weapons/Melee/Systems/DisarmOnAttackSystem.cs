// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Weapons.Melee.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.CombatMode;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Weapons.Melee.Systems;

public sealed class SharedDisarmOnAttackSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisarmOnAttackComponent, WeaponAttackEvent>(OnAttackEvent);
    }

    private void OnAttackEvent(Entity<DisarmOnAttackComponent> ent, ref WeaponAttackEvent args)
    {
        float chance = 0;
        switch (args.Type)
        {
            case AttackType.HEAVY:
                chance = ent.Comp.HeavyAttackChance;
                break;

            case AttackType.LIGHT:
                chance = ent.Comp.Chance;
                break;
        }

        if (chance <= 0)
            return;

        var ev = new DisarmedEvent(args.Target, args.User, chance);
        RaiseLocalEvent(args.Target, ref ev);
    }
}
