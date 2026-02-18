// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;

namespace Content.Shared.FCB.ArmorBlock;

public sealed class ArmorBlockSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArmorBlockComponent, DamageModifyEvent>(OnDamageChange);

        SubscribeLocalEvent<ArmorBlockComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<ArmorBlockComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<ArmorBlockComponent, DroppedEvent>(OnDrop);
    }

    public void OnDamageChange(Entity<ArmorBlockComponent> ent, ref DamageModifyEvent args)
    {
        if (args.OriginalDamage == null || ent.Comp.Owner == null) { return; }

        FixedPoint2 maximalDamage = 0;
        string? maximalDamageType = null;

        var resultDamage = new DamageSpecifier();
        var resultArmorDamage = new DamageSpecifier();

        foreach (var type in args.OriginalDamage.DamageDict.Keys)//Here we start counting damage for each type
        {
            if(ent.Comp.DurabilityTresholdDict.ContainsKey(type))
                CountDifference(
                    resultArmorDamage.DamageDict,
                    args.OriginalDamage.DamageDict[type],
                    ent.Comp.DurabilityTresholdDict[type],
                    type,
                    piercing: args.OriginalDamage.armourPiercing);//armor damage

            else
                resultArmorDamage.DamageDict.Add(type, args.OriginalDamage.DamageDict[type]);

            if(ent.Comp.TresholdDict.ContainsKey(type))
            {
                var damageDiff = CountDifference(
                    resultDamage.DamageDict,
                    args.OriginalDamage.DamageDict[type],
                    ent.Comp.TresholdDict[type],
                    type,
                    args.OriginalDamage.armourPiercing);//user damage

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
                        ent.Comp.TransformSpecifierDict[type], FixedPoint2.Zero); //Piercing is not applied here

                continue;

            }

            CountDifference(resultDamage.DamageDict, args.OriginalDamage.DamageDict[type], FixedPoint2.Zero, type, FixedPoint2.Zero);
        }
        args.Damage = resultArmorDamage;

        if(maximalDamageType != null)
        {
            if (args.OriginalDamage.armourPiercing > ent.Comp.TresholdDict[maximalDamageType])
            {
                resultDamage.armourPiercing = args.OriginalDamage.armourPiercing - ent.Comp.TresholdDict[maximalDamageType];
                _damageable.TryChangeDamage((EntityUid)ent.Comp.Owner, resultDamage);
                return;
            }
            resultDamage.armourPiercing = 0;
        }

        _damageable.TryChangeDamage((EntityUid)ent.Comp.Owner, resultDamage);
    }

    public FixedPoint2 CountDifference(Dictionary<string,FixedPoint2> dict,FixedPoint2 damage, FixedPoint2 resist,string type, FixedPoint2 piercing)
    {
        resist = resist - piercing;

        if (resist < 0)
            resist = 0;

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

    public void OnEquip(Entity<ArmorBlockComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.Owner = args.User;
    }

    public void OnUnequip(Entity<ArmorBlockComponent> ent, ref GotUnequippedHandEvent args)
    {
        ent.Comp.Owner = null;
    }

    public void OnDrop(Entity<ArmorBlockComponent> ent, ref DroppedEvent args)
    {
        ent.Comp.Owner = null;
    }
}
