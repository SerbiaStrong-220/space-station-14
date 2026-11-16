// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.CombatMode;

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
        var chance = args.Type switch
        {
            AttackType.HEAVY => ent.Comp.HeavyAttackChance,
            AttackType.LIGHT => ent.Comp.Chance,
            _ => 0,
        };

        if (chance <= 0)
            return;

        var ev = new DisarmedEvent(args.Target, args.User, chance);
        RaiseLocalEvent(args.Target, ref ev);
    }
}
