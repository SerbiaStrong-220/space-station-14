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
                    continue;
                default:
                    entityBlockDamageComponent.Duration -= frameTime;
                    break;
            }
        }
    }

    private void OnDamageModify(Entity<EntityBlockDamageComponent> ent, ref DamageModifyEvent args)
    {
        Dictionary<string, FixedPoint2> newDict = [];

        foreach (var (type, value) in args.Damage.DamageDict)
        {
            var modifiedValue = value;

            if (ent.Comp.BlockAllTypesDamage)
            {
                if (ent.Comp.DamageCoefficient > 0)
                    modifiedValue *= ent.Comp.DamageCoefficient;
                else
                {
                    args.Damage = new DamageSpecifier();
                    return;
                }
            }

            if (ent.Comp.Modifiers?.Coefficients.TryGetValue(type, out var coeff) == true)
            {
                modifiedValue *= coeff;
            }

            newDict[type] = modifiedValue;
        }

        args.Damage.DamageDict = newDict;
    }
}
