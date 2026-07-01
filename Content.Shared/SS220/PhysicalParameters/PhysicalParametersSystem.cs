// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Clothing;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Grab;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.SS220.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee;

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
        SubscribeLocalEvent<PhysicalParametersComponent, GrabDelayModifiersEvent>(OnGrabAttempt);

        base.Initialize();
    }

    public void OnMeleeAttack(Entity<PhysicalParametersComponent> ent, ref MeleeAttackerEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(args.Used, out var weaponComp) ||
            !weaponComp.StrengthAffectsDamage)
            return;

        FixedPoint2 strengthModifier = GetParameterValue(ent, Parameter.Strength);

        if (HasComp<ItemExtensionMeleeWeaponComponent>(args.Used) && TryComp<ItemExtensionComponent>(args.Used, out var extensionComp))
        {
            FixedPoint2 toDivide = extensionComp.StrengthRequirementToBeUsed - extensionComp.MinimalStrengthToPickUp;

            if (toDivide == 0)
                toDivide = 1;

            strengthModifier = (strengthModifier - extensionComp.MinimalStrengthToPickUp) / toDivide;
        }

        foreach (var type in args.Damage.DamageDict)
        {
            if (weaponComp.AffectedDamageTypes.Contains(type.Key))
            {
                if (args.ModifiedDamage.DamageDict.ContainsKey(type.Key))
                {
                    args.ModifiedDamage.DamageDict[type.Key] += type.Value + (type.Value * strengthModifier - type.Value) * weaponComp.StrengthDamageMultiplier;
                    continue;
                }

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

    public void OnGrabAttempt(Entity<PhysicalParametersComponent> ent, ref GrabDelayModifiersEvent args)
    {
        FixedPoint2 grabbedStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.Grabbable, out var grabbedComp))
            grabbedStrength = GetParameterValue((args.Grabbable, grabbedComp), Parameter.Strength);

        args.Multiply((grabbedStrength / GetParameterValue(ent, Parameter.Strength)).Float());
    }

    public FixedPoint2 GetParameterValue(Entity<PhysicalParametersComponent> ent, Parameter parameter)
    {
        FixedPoint2 strengthModifier = 1f;

        if (ent.Comp.StrengthAffectsArms &&
            ent.Comp.ParameterDict.ContainsKey(parameter))
        {
            strengthModifier = ent.Comp.ParameterDict[parameter];

            if (TryComp<HumanoidProfileComponent>(ent.Owner, out var profileComp) && profileComp.Sex == Sex.Female)
                if (ent.Comp.GenderModifier.TryGetValue(parameter, out var genderModifier))
                    strengthModifier += genderModifier;
        }

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

        var evHeld = new UserParametersChangedEvent();

        foreach (var item in _handsSystem.EnumerateHeld(ent.Owner))
            RaiseLocalEvent(item, ref evHeld);

        Dirty(ent);

        _movementSystem.RefreshMovementSpeedModifiers(ent);
    }

    public void SetParameter(Entity<PhysicalParametersComponent> ent, Parameter parameter, FixedPoint2 value)
    {
        if (ent.Comp.ParameterDict.ContainsKey(parameter))
            ent.Comp.ParameterDict[parameter] = value;

        var ev = new ParametersChangedEvent();

        RaiseLocalEvent(ent, ref ev);

        var evHeld = new UserParametersChangedEvent();

        foreach (var item in _handsSystem.EnumerateHeld(ent.Owner))
            RaiseLocalEvent(item, ref evHeld);

        Dirty(ent);

        _movementSystem.RefreshMovementSpeedModifiers(ent);
    }

    [ByRefEvent]
    public readonly record struct ParametersChangedEvent()
    {
    }

    [ByRefEvent]
    public readonly record struct UserParametersChangedEvent()
    {
    }
}
