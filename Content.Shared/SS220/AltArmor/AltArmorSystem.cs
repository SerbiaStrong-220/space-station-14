// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.AltArmor.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.AltArmor;

public sealed class AltArmorSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public void ModifyDamage(Entity<AltArmorComponent?> ent, DamageSpecifier? damage, out DamageSpecifier resultDamage, out DamageSpecifier resultArmorDamage)
    {
        resultDamage = new DamageSpecifier();
        resultArmorDamage = new DamageSpecifier();

        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (damage == null)
            return;

        FixedPoint2 maximalDamage = 0;
        string? maximalDamageType = null;

        FixedPoint2 durabilityCoefficient = 1;

        if (TryComp<DamageableComponent>(ent, out var damageableComp) && ent.Comp.DamageAffectsProtection)
        {
            durabilityCoefficient = 1 - (_damageable.GetTotalDamage(ent.Owner) / ent.Comp.ZeroProtectionThreshold);

            if (durabilityCoefficient < 0)//Didn't use Math.Clamp because there is no override of this function for FixedPoint2 and i don't want to convert types 2 times every time we calculate this
                durabilityCoefficient = 0;

            if (durabilityCoefficient > 1)
                durabilityCoefficient = 1;
        }

        foreach (var type in damage.DamageDict.Keys)//Here we start counting damage for each type
        {
            if (ent.Comp.DurabilityTresholdDict.ContainsKey(type))
                CountDifference(
                    resultArmorDamage.DamageDict,
                    damage.DamageDict[type],
                    ent.Comp.DurabilityTresholdDict[type],
                    type,
                    piercing: damage.ArmourPiercing,
                    durabilityCoefficient: durabilityCoefficient
                );//armor damage

            else
                resultArmorDamage.DamageDict.Add(type, damage.DamageDict[type]);

            if (ent.Comp.TresholdDict.ContainsKey(type))
            {
                var damageDiff = CountDifference(
                    resultDamage.DamageDict,
                    damage.DamageDict[type],
                    ent.Comp.TresholdDict[type],
                    type,
                    damage.ArmourPiercing,
                    durabilityCoefficient: durabilityCoefficient
                );//user damage

                if (damageDiff > maximalDamage)
                {
                    maximalDamage = damageDiff;
                    maximalDamageType = type;
                }

                if (ent.Comp.TransformSpecifierDict.ContainsKey(type) && ent.Comp.TresholdDict.ContainsKey(ent.Comp.TransformSpecifierDict[type]))
                    CountDifference(
                        resultDamage.DamageDict,
                        damage.DamageDict[type] - damageDiff,
                        ent.Comp.TresholdDict[ent.Comp.TransformSpecifierDict[type]],
                        ent.Comp.TransformSpecifierDict[type], FixedPoint2.Zero,
                        durabilityCoefficient: durabilityCoefficient
                    ); //Piercing is not applied here

                continue;

            }

            CountDifference(resultDamage.DamageDict, damage.DamageDict[type], FixedPoint2.Zero, type, FixedPoint2.Zero, durabilityCoefficient: durabilityCoefficient);
        }

        if (ent.Owner == null)
            return;

        if(maximalDamageType != null)
        {
            if (damage.ArmourPiercing > ent.Comp.TresholdDict[maximalDamageType])// A kostyl made to lower the piercing stat to prevent infinite/too good penetration of anything
            {
                resultDamage.ArmourPiercing = damage.ArmourPiercing - ent.Comp.TresholdDict[maximalDamageType];
                _damageable.TryChangeDamage(ent.Owner, resultDamage);
                return;
            }
            resultDamage.ArmourPiercing = 0;
        }
    }

    public FixedPoint2 CountDifference(Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> dict, FixedPoint2 damage, FixedPoint2 resist,string type, FixedPoint2 piercing, FixedPoint2 durabilityCoefficient)
    {
        resist *= durabilityCoefficient;
        resist = FixedPoint2.Max(resist - piercing, FixedPoint2.Zero);

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
