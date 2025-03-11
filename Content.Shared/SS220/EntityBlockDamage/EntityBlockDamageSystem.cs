using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.EntityBlockDamage;

public sealed class EntityBlockDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<EntityBlockDamageComponent, DamageModifyEvent>(OnDamageModify);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<EntityBlockDamageComponent>();

        while (query.MoveNext(out var target, out var entityBlockDamageComponent))
        {
            switch (entityBlockDamageComponent.Duration)
            {
                case null:
                    continue;
                case <= 0:
                    RemCompDeferred<EntityBlockDamageComponent>(target);
                    break;
            }

            entityBlockDamageComponent.Duration -= frameTime;
        }
    }

    private void OnDamageModify(Entity<EntityBlockDamageComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.Modifiers != null)
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.Modifiers);

        if (ent.Comp is not { BlockAllDamage: true, BlockPercent: not null })
            return;

        Dictionary<string, FixedPoint2> newDictDamage = [];

        foreach (var (type, value) in args.Damage.DamageDict)
        {
            var reducedValue = value * (1 - ent.Comp.BlockPercent.Value);
            newDictDamage[type] = reducedValue;
        }

        args.Damage.DamageDict = newDictDamage;
    }
}
