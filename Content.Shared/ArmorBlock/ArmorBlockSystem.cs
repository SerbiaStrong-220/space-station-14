using Content.Shared.Blocking;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if(args.OriginalDamage.DamageDict[type] > ent.Comp.DurabilityTresholdDict[type])
                {
                    if (resultArmorDamage.DamageDict.ContainsKey(type))
                    {
                        resultArmorDamage.DamageDict[type] += args.OriginalDamage.DamageDict[type] - ent.Comp.DurabilityTresholdDict[type];
                    }
                    else { resultArmorDamage.DamageDict.Add(type, args.OriginalDamage.DamageDict[type] - ent.Comp.DurabilityTresholdDict[type]); }
                }
            }
            else { resultArmorDamage.DamageDict.Add(type, args.OriginalDamage.DamageDict[type]); }
            if(ent.Comp.TresholdDict.ContainsKey(type))
            {
                if (ent.Comp.TresholdDict[type] <= args.OriginalDamage.DamageDict[type])
                {
                    if (resultDamage.DamageDict.ContainsKey(type))
                    {
                        resultDamage.DamageDict[type] += args.OriginalDamage.DamageDict[type] - ent.Comp.TresholdDict[type];
                    }
                    else { resultDamage.DamageDict.Add(type, args.OriginalDamage.DamageDict[type] - ent.Comp.TresholdDict[type]); }
                }
                if (ent.Comp.TransformSpecifierDict.ContainsKey(type))
                {
                    if (ent.Comp.TresholdDict.ContainsKey(ent.Comp.TransformSpecifierDict[type]) && args.OriginalDamage.DamageDict[type] > ent.Comp.TresholdDict[ent.Comp.TransformSpecifierDict[type]])
                    {
                        var convertedDamage = new DamageSpecifier();
                        if (resultDamage.DamageDict.ContainsKey(ent.Comp.TransformSpecifierDict[type]))
                        {
                            resultDamage.DamageDict[ent.Comp.TransformSpecifierDict[type]] += (args.OriginalDamage.DamageDict[ent.Comp.TransformSpecifierDict[type]] - ent.Comp.TresholdDict[ent.Comp.TransformSpecifierDict[type]]);
                        }
                        else { resultDamage.DamageDict.Add(ent.Comp.TransformSpecifierDict[type], args.OriginalDamage.DamageDict[type] - ent.Comp.TresholdDict[ent.Comp.TransformSpecifierDict[type]]); }
                    }
                    var transformedDamage = new DamageSpecifier();
                    transformedDamage.DamageDict.Add(ent.Comp.TransformSpecifierDict[type], args.OriginalDamage.DamageDict[type]);
                    _damageable.TryChangeDamage(ent.Owner, transformedDamage);
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
