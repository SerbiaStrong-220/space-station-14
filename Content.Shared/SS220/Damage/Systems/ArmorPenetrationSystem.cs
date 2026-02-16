using Content.Shared.FixedPoint;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.SS220.Damage.Components;
using Content.Shared.SS220.Damage.Events;
using Robust.Shared.Prototypes;
using System.Text;

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

    /// <summary>
    /// Used to get entity's armor penetration values, if present
    /// </summary>
    /// <returns>ArmorPenetrationComponent</returns>
    public ArmorPenetrationComponent? GetArmorPenetration(EntProtoId proto)
    {
        if (!_prototypeManager.TryIndex<EntityPrototype>(proto, out var entityProto))
            return null;

        if (entityProto.TryGetComponent<ArmorPenetrationComponent>(out var apComp, Factory))
            return apComp;

        return null;
    }

    public string BuildArmorPenetrationDescription(List<ArmorPenetrationRule> rules)
    {
        if (rules.Count == 0)
            return "";

        var description = new StringBuilder();
        description.AppendLine(Loc.GetString("ammo-ap-rules-header"));

        foreach (var rule in rules)
        {
            string damageTypeName;
            if (_prototypeManager.TryIndex<DamageTypePrototype>(rule.DamageType, out var damageTypeProto))
                damageTypeName = damageTypeProto.LocalizedName;

            else
                damageTypeName = rule.DamageType; // Use ID as fallback

            var multiplierPercentage = (rule.Multiplier * 100.0f).ToString("F0");

            var armorConditionStr = rule.Reversed ? "less" : "more" ;
            var thresholdPercentage = (100.0f - rule.ArmorThreshold * 100.0f).ToString("F0");

            var ruleDescription = Loc.GetString("ammo-ap-rule-description",
                                               ("type", damageTypeName),
                                               ("multiplier", multiplierPercentage),
                                               ("armor", armorConditionStr),
                                               ("threshold", thresholdPercentage));
            description.AppendLine(ruleDescription);
        }

        // Remove the trailing newline added by the last AppendLine
        var finalString = description.ToString();
        if (finalString.EndsWith("\n"))
            finalString = finalString[..^1];

        return finalString;
    }

}
