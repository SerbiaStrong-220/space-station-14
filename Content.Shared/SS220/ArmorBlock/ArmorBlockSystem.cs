using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;

namespace Content.Shared.SS220.ArmorBlock;

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
    public void OnDamageChange(Entity<ArmorBlockComponent> ent,ref DamageModifyEvent args)
    {
        if (args.OriginalDamage == null || ent.Comp.Owner == null) { return; }
        var resultDamage = new DamageSpecifier();
        var resultArmorDamage = new DamageSpecifier();
        foreach (var type in args.OriginalDamage.DamageDict.Keys)
        {
            if(ent.Comp.DurabilityTresholdDict.ContainsKey(type))
            {
                CountDifference(resultArmorDamage.DamageDict, args.OriginalDamage.DamageDict[type], ent.Comp.DurabilityTresholdDict[type], type);
            }
            else { resultArmorDamage.DamageDict.Add(type, args.OriginalDamage.DamageDict[type]); }
            if(ent.Comp.TresholdDict.ContainsKey(type))
            {
                CountDifference(resultDamage.DamageDict, args.OriginalDamage.DamageDict[type], ent.Comp.TresholdDict[type], type);
                if (ent.Comp.TransformSpecifierDict.ContainsKey(type))
                {
                    CountDifference(resultDamage.DamageDict, args.OriginalDamage.DamageDict[type], ent.Comp.TresholdDict[ent.Comp.TransformSpecifierDict[type]], ent.Comp.TransformSpecifierDict[type]);
                }
                args.OriginalDamage.DamageDict[type] = 0f;
            }
            else
            {
                if (resultDamage.DamageDict.ContainsKey(type))
                {
                    resultDamage.DamageDict[type] += args.OriginalDamage.DamageDict[type];
                }
                else { resultDamage.DamageDict.Add(type, args.OriginalDamage.DamageDict[type]); }
            }
        }
        args.Damage = resultArmorDamage;
        _damageable.TryChangeDamage(ent.Comp.Owner, resultDamage);
    }

    public FixedPoint2 CountDifference(Dictionary<string,FixedPoint2> dict,FixedPoint2 damage, FixedPoint2 resist,string type)
    {
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
