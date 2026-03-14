// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.FCB.ArmorBlock;

public sealed class ArmorBlockSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArmorBlockComponent, DamageModifyEvent>(OnDamageChange);
    }

    public void OnDamageChange(Entity<ArmorBlockComponent> ent, ref DamageModifyEvent args)
    {
        if (args.OriginalDamage == null)
            return;

        FixedPoint2 maximalDamage = 0;
        string? maximalDamageType = null;

        var resultDamage = new DamageSpecifier();
        var resultArmorDamage = new DamageSpecifier();

        FixedPoint2 durabilityCoefficient = 1;

        if (TryComp<DamageableComponent>(ent, out var damageableComp) && ent.Comp.DamageAffectsProtection)
        {
            durabilityCoefficient = 1 - (damageableComp.TotalDamage / ent.Comp.ZeroProtectionThreshold);

            if (durabilityCoefficient < 0)//Didn't use Math.Clamp because there is no override of this function for FixedPoint2 and i don't want to convert types 2 times every time we calculate this
                durabilityCoefficient = 0;
        }

        foreach (var type in args.OriginalDamage.DamageDict.Keys)//Here we start counting damage for each type
        {
            if (ent.Comp.DurabilityTresholdDict.ContainsKey(type))
                CountDifference(
                    resultArmorDamage.DamageDict,
                    args.OriginalDamage.DamageDict[type],
                    ent.Comp.DurabilityTresholdDict[type],
                    type,
                    piercing: args.OriginalDamage.ArmourPiercing,
                    durabilityCoefficient: durabilityCoefficient
                );//armor damage

            else
                resultArmorDamage.DamageDict.Add(type, args.OriginalDamage.DamageDict[type]);

            if (ent.Comp.TresholdDict.ContainsKey(type))
            {
                var damageDiff = CountDifference(
                    resultDamage.DamageDict,
                    args.OriginalDamage.DamageDict[type],
                    ent.Comp.TresholdDict[type],
                    type,
                    args.OriginalDamage.ArmourPiercing,
                    durabilityCoefficient: durabilityCoefficient
                );//user damage

                if (damageDiff > maximalDamage)
                {
                    maximalDamage = damageDiff;
                    maximalDamageType = type;
                }

                if (ent.Comp.TransformSpecifierDict.ContainsKey(type))
                    CountDifference(
                        resultDamage.DamageDict,
                        args.OriginalDamage.DamageDict[type],
                        ent.Comp.TresholdDict[ent.Comp.TransformSpecifierDict[type]],
                        ent.Comp.TransformSpecifierDict[type], FixedPoint2.Zero,
                        durabilityCoefficient: durabilityCoefficient
                    ); //Piercing is not applied here

                continue;

            }

            CountDifference(resultDamage.DamageDict, args.OriginalDamage.DamageDict[type], FixedPoint2.Zero, type, FixedPoint2.Zero, durabilityCoefficient: durabilityCoefficient);
        }
        args.Damage = resultArmorDamage;

        if (ent.Comp.Owner == null)
            return;

        if(maximalDamageType != null)
        {
            if (args.OriginalDamage.ArmourPiercing > ent.Comp.TresholdDict[maximalDamageType])// A kostyl made to lower the piercing stat to prevent infinite/too good penetration of anything
            {
                resultDamage.ArmourPiercing = args.OriginalDamage.ArmourPiercing - ent.Comp.TresholdDict[maximalDamageType];
                _damageable.TryChangeDamage((EntityUid)ent.Comp.Owner, resultDamage);
                return;
            }
            resultDamage.ArmourPiercing = 0;
        }

        _damageable.TryChangeDamage((EntityUid)ent.Comp.Owner, resultDamage);
    }

    public FixedPoint2 CountDifference(Dictionary<string, FixedPoint2> dict, FixedPoint2 damage, FixedPoint2 resist,string type, FixedPoint2 piercing, FixedPoint2 durabilityCoefficient)
    {
        resist = Math.Clamp(resist.Float() - piercing.Float(), 0f, Math.Abs(resist.Float()) + Math.Abs(piercing.Float()));

        if (damage > resist)
        {
            if (dict.ContainsKey(type))
            {
                dict[type] += damage - resist;
                return damage - resist;
            }

            dict.Add(type, damage - resist);
            return damage - resist;
        }
        return 0;
    }
}
