using Content.Shared.FixedPoint;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.SS220.Damage.Components;
using Content.Shared.SS220.Damage.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Damage.Systems;

public sealed class ArmorPenetrationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<APDamageModifyEvent>(OnAPDamageModify);
    }

    private void OnAPDamageModify(ref APDamageModifyEvent args)
    {
        if (args.Source == null)
            return;
        
        if (!TryComp<ArmorPenetrationComponent>(args.Source.Value, out var apComp))
            return;

        var target = args.Target;
                
        var targetCoefficients = GetDamageCoefficients(target);
                
        var damage = args.Damage;
        var newDamageDict = new Dictionary<string, FixedPoint2>(damage.DamageDict);

        foreach (var rule in apComp.Rules)
        {
            if (!newDamageDict.TryGetValue(rule.DamageType, out var currentValue))
                continue;

            var targetCoefficient = targetCoefficients.GetValueOrDefault(rule.DamageType, 1.0f);
            var conditionMet = rule.Reversed
                ? targetCoefficient > rule.ArmorThreshold
                : targetCoefficient <= rule.ArmorThreshold;

            if (conditionMet)
            {
                var oldValue = currentValue;
                var multiplier = FixedPoint2.New(rule.Multiplier);
                var newValue = currentValue * multiplier;
                newDamageDict[rule.DamageType] = newValue;
            }
        }
        args.Damage = new DamageSpecifier { DamageDict = newDamageDict };
    }

    // this approach is kinda goida
    /// <summary>
    /// Total damage coefficients, from entity itself ant it's armor.
    /// </summary>
    private Dictionary<string, float> GetDamageCoefficients(EntityUid target)
    {
        var coefficients = new Dictionary<string, float>();
        
        // Entity itself
        if (TryComp<DamageableComponent>(target, out var damageable) 
            && damageable.DamageModifierSetId != null
            && _prototypeManager.TryIndex<DamageModifierSetPrototype>(damageable.DamageModifierSetId, out var modifierSet))
        {
            foreach (var (dmgType, coeffValue) in modifierSet.Coefficients)
            {
                coefficients[dmgType] = coeffValue;
            }
        }
        
        // Armor
        if (!_inventory.TryGetSlots(target, out var slots))
            return coefficients;

        foreach (var slot in slots)
        {
            if ((slot.SlotFlags & SlotFlags.POCKET) != 0)
                continue;

            if (!_inventory.TryGetSlotEntity(target, slot.Name, out var equipUid))
                continue;

            if (!TryComp<ArmorComponent>(equipUid.Value, out var armor))
                continue;

            foreach (var (dmgType, coeffValue) in armor.Modifiers.Coefficients)
            {
                if (coefficients.TryGetValue(dmgType, out var existing))
                    coefficients[dmgType] = existing * coeffValue;
                else
                    coefficients[dmgType] = coeffValue;
            }
        }
        
        return coefficients;
    }
}
