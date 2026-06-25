// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Clothing;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class PhysicalParametersSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PhysicalParametersComponent, MeleeAttackerEvent>(OnMeleeAttack);
        SubscribeLocalEvent<PhysicalParametersModifyingClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<PhysicalParametersModifyingClothingComponent, GotUnequippedEvent>(OnGotUnequipped);

        base.Initialize();
    }

    public void OnMeleeAttack(Entity<PhysicalParametersComponent> ent, ref MeleeAttackerEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(args.Used, out var weaponComp) ||
            !weaponComp.StrengthAffectsDamage)
            return;

        FixedPoint2 strengthModifier = GetParameterValue(ent, Parameter.Strength);

        if (TryComp<ItemExtensionComponent>(args.Used, out var extensionComp))
            strengthModifier = (strengthModifier - extensionComp.MinimalStrengthToPickUp) / (extensionComp.StrengthRequirementToBeUsed - 1);

        foreach (var type in args.Damage.DamageDict)
        {
            if (weaponComp.AffectedDamageTypes.Contains(type.Key))
            {
                args.ModifiedDamage.DamageDict.Add(type.Key, type.Value + (type.Value * strengthModifier - type.Value) * weaponComp.StrengthDamageMultiplier);
                continue;
            }

            args.ModifiedDamage.DamageDict.Add(type.Key, type.Value);
        }
    }

    public void OnGotEquipped(Entity<PhysicalParametersModifyingClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!TryComp<PhysicalParametersComponent>(args.Wearer, out var parametersComp))
            return;

        foreach (var parameter in ent.Comp.ParameterDict)
        {
            if (!parametersComp.ParameterDict.ContainsKey(parameter.Key))
                continue;

            if (ent.Comp.AddParameters)
            {
                AddParameter((args.Wearer, parametersComp), parameter.Key, parameter.Value);
                continue;
            }

            SetParameter((args.Wearer, parametersComp), parameter.Key, parameter.Value);
        }
    }

    public void OnGotUnequipped(Entity<PhysicalParametersModifyingClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (!TryComp<PhysicalParametersComponent>(args.EquipTarget, out var parametersComp))
            return;

        foreach (var parameter in ent.Comp.ParameterDict)
        {
            if (!parametersComp.ParameterDict.ContainsKey(parameter.Key))
                continue;

            if (ent.Comp.AddParameters)
            {
                AddParameter((args.EquipTarget, parametersComp), parameter.Key, -parameter.Value);
                continue;
            }

            SetParameter((args.EquipTarget, parametersComp), parameter.Key, 1);
        }
    }

    public FixedPoint2 GetParameterValue(Entity<PhysicalParametersComponent> ent, Parameter parameter)
    {
        FixedPoint2 strengthModifier = 1f;

        if (ent.Comp.StrengthAffectsArms &&
            ent.Comp.ParameterDict.ContainsKey(Parameter.Strength))
            strengthModifier = ent.Comp.ParameterDict[Parameter.Strength];

        if (TryComp<HandsComponent>(ent.Owner, out var handsComp) &&
            handsComp.ActiveHandId != null &&
            _handsSystem.TryGetHand(ent.Owner, handsComp.ActiveHandId, out var activeHand) &&
            activeHand.Value.StrengthModifier != null)
            strengthModifier = (FixedPoint2)activeHand.Value.StrengthModifier;

        return strengthModifier;
    }

    public void AddParameter(Entity<PhysicalParametersComponent> ent, Parameter parameter, FixedPoint2 value)
    {
        if (ent.Comp.ParameterDict.ContainsKey(parameter))
            ent.Comp.ParameterDict[parameter] += value;

        var ev = new ParametersChangedEvent();

        RaiseLocalEvent(ent, ref ev);

        _movementSystem.RefreshMovementSpeedModifiers(ent);
    }

    public void SetParameter(Entity<PhysicalParametersComponent> ent, Parameter parameter, FixedPoint2 value)
    {
        if (ent.Comp.ParameterDict.ContainsKey(parameter))
            ent.Comp.ParameterDict[parameter] = value;

        var ev = new ParametersChangedEvent();

        RaiseLocalEvent(ent, ref ev);

        _movementSystem.RefreshMovementSpeedModifiers(ent);
    }

    [ByRefEvent]
    public readonly record struct ParametersChangedEvent()
    {
    }
}
