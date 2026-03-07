using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.SS220.Lifesteal;

public sealed class LifestealSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LifestealComponent, WeaponAttackEvent>(OnHealOnAttack);
    }

    private void OnHealOnAttack(Entity<LifestealComponent> ent, ref WeaponAttackEvent args)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageable))
            return;

        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (_mobState.IsDead(args.Target))
            return;

        var totalDamage = damageable.TotalDamage;
        if (totalDamage == FixedPoint2.Zero)
            return;

        var damageDict = damageable.Damage.DamageDict;
        var healSpec = new DamageSpecifier();

        foreach (var (group, amount) in damageDict)
        {
            if (amount <= 0)
                continue;

            var ratio = amount / totalDamage;
            var healPerGroup = ratio * ent.Comp.Lifesteal;

            healSpec.DamageDict[group] = -healPerGroup;
        }

        _damageable.TryChangeDamage(ent, healSpec, true);
    }

    public void ChangeLifesteal(Entity<LifestealComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Lifesteal = amount;
        Dirty(ent);
    }
}
