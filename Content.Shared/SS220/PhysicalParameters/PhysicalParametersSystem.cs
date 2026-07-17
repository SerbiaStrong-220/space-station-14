// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Clothing;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Grab;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.SS220.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Wieldable.Components;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class PhysicalParametersSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSystem = default!;

    private readonly FixedPoint2 UngrabbableStrengthDifference = 1.7;

    public override void Initialize()
    {
        SubscribeLocalEvent<PhysicalParametersComponent, MeleeAttackerEvent>(OnMeleeAttack);

        SubscribeLocalEvent<PhysicalParametersComponent, GrabDelayModifiersEvent>(OnGrabAttempt);
        SubscribeLocalEvent<PhysicalParametersComponent, GrabBreakChanceModifyEvent>(OnGrabBreakAttempt);
        SubscribeLocalEvent<PhysicalParametersComponent, GrabCancelEvent>(OnGrabCancel);

        SubscribeLocalEvent<PhysicalParametersComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<PhysicalParametersComponent, SexChangedEvent>(OnSexChanged);

        SubscribeLocalEvent<PhysicalParametersModifyingClothingComponent, InventoryRelayedEvent<ParametersUpdateEvent>>(OnUpdateRelayedEvent);
        SubscribeLocalEvent<PhysicalParametersModifyingClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<PhysicalParametersModifyingClothingComponent, GotUnequippedEvent>(OnGotUnequipped);

        base.Initialize();
    }

    public void OnCompInit(Entity<PhysicalParametersComponent> ent, ref ComponentInit args)
    {
        UpdateParameterValues(ent);
    }

    public void OnSexChanged(Entity<PhysicalParametersComponent> ent, ref SexChangedEvent args)
    {
        UpdateParameterValues(ent);
    }

    public void OnGrabCancel(Entity<PhysicalParametersComponent> ent, ref GrabCancelEvent args)
    {
        if (args.Cancelled)
            return;

        FixedPoint2 grabbedStrength = GetParameterValue(ent, Parameter.Strength);
        FixedPoint2 grabberStrength = 1;

        if (args.Grabber is not { Valid: true } grabberValidated)
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<PhysicalParametersComponent>(grabberValidated, out var grabberComp))
            grabberStrength = GetParameterValue((grabberValidated, grabberComp), Parameter.Strength);

        if (grabbedStrength >= grabberStrength * UngrabbableStrengthDifference)
        {
            args.Cancelled = true;
            return;
        }
    }

    public void OnMeleeAttack(Entity<PhysicalParametersComponent> ent, ref MeleeAttackerEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(args.Used, out var weaponComp) ||
            !weaponComp.StrengthAffectsDamage)
            return;

        FixedPoint2 strengthModifier = GetParameterValue(ent, Parameter.Strength);

        if (TryComp<WieldableComponent>(args.Used, out var wieldableComp) && wieldableComp.Wielded)
            strengthModifier += strengthModifier * wieldableComp.FreeHandsRequired;

        if (TryComp<MultiHandedItemComponent>(args.Used, out var multiHandedComp))
            strengthModifier += strengthModifier * multiHandedComp.HandsNeeded;

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

    public void OnUpdateRelayedEvent(Entity<PhysicalParametersModifyingClothingComponent> ent, ref InventoryRelayedEvent<ParametersUpdateEvent> args)
    {
        foreach (var (parameter, value) in ent.Comp.ParameterDict)
        {
            if (!args.Args.ModifiedValues.ContainsKey(parameter))
            {
                args.Args.ModifiedValues.Add(parameter, value);
                continue;
            }

            args.Args.ModifiedValues[parameter] += value;
        }
    }

    public void OnGotEquipped(Entity<PhysicalParametersModifyingClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!TryComp<PhysicalParametersComponent>(args.Wearer, out var parametersComp))
            return;

        UpdateParameterValues((args.Wearer, parametersComp));
    }

    public void OnGotUnequipped(Entity<PhysicalParametersModifyingClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (!TryComp<PhysicalParametersComponent>(args.EquipTarget, out var parametersComp))
            return;

        UpdateParameterValues((args.EquipTarget, parametersComp));
    }

    public void OnGrabAttempt(Entity<PhysicalParametersComponent> ent, ref GrabDelayModifiersEvent args)
    {
        FixedPoint2 grabbedStrength = 1;

        if (TryComp<PhysicalParametersComponent>(args.Grabbable, out var grabbedComp))
            grabbedStrength = GetParameterValue((args.Grabbable, grabbedComp), Parameter.Strength);

        args.Multiply((grabbedStrength / GetParameterValue(ent, Parameter.Strength)).Float());
    }

    public void OnGrabBreakAttempt(Entity<PhysicalParametersComponent> ent, ref GrabBreakChanceModifyEvent args)
    {
        if (args.Grabber is not { Valid: true } grabberValidated)
        {
            args.Chance += 1;
            return;
        }

        FixedPoint2 grabbedStrength = GetParameterValue(ent, Parameter.Strength);
        FixedPoint2 grabberStrength = 1;

        if (TryComp<PhysicalParametersComponent>(grabberValidated, out var grabberComp))
            grabberStrength = GetParameterValue((grabberValidated, grabberComp), Parameter.Strength);

        if (grabbedStrength >= grabberStrength * UngrabbableStrengthDifference)
        {
            args.Chance += 1;
            return;
        }

        args.Chance *= (grabbedStrength / grabberStrength).Float();
    }


    public FixedPoint2 GetParameterValue(Entity<PhysicalParametersComponent> ent, Parameter parameter, bool armStrengthCounted = true)
    {
        FixedPoint2 strengthModifier = 1f;

        if (ent.Comp.StrengthAffectsArms &&
            ent.Comp.ParameterDictModified.ContainsKey(parameter))
            strengthModifier = ent.Comp.ParameterDictModified[parameter];

        if (armStrengthCounted &&
            TryComp<HandsComponent>(ent.Owner, out var handsComp) &&
            handsComp.ActiveHandId != null &&
            _handsSystem.TryGetHand(ent.Owner, handsComp.ActiveHandId, out var activeHand) &&
            activeHand.Value.StrengthModifier != null)
            strengthModifier = (FixedPoint2)activeHand.Value.StrengthModifier;

        return strengthModifier;
    }

    public void UpdateParameterValues(Entity<PhysicalParametersComponent> ent)
    {
        ent.Comp.ParameterDictModified = new Dictionary<Parameter, FixedPoint2>(ent.Comp.ParameterDict);

        var ev = new ParametersUpdateEvent();

        RaiseLocalEvent(ent, ref ev);

        if (TryComp<HumanoidProfileComponent>(ent.Owner, out var profileComp) && profileComp.Sex == Sex.Female)
        {
            foreach (var (parameter, value) in ent.Comp.GenderModifier)
            {
                if (ent.Comp.ParameterDictModified.ContainsKey(parameter))
                {
                    ent.Comp.ParameterDictModified[parameter] += value;
                    continue;
                }

                ent.Comp.ParameterDictModified.Add(parameter, value);
            }
        }

        foreach (var (parameter, value) in ev.ModifiedValues)
        {
            var valueToAdd = value;

            if (ent.Comp.ParameterDictModified.ContainsKey(parameter))
            {
                ent.Comp.ParameterDictModified[parameter] += valueToAdd;
                continue;
            }

            ent.Comp.ParameterDictModified.Add(parameter, valueToAdd);
        }
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

        var evHeld = new UserParametersChangedEvent(ent.Owner);

        foreach (var item in _handsSystem.EnumerateHeld(ent.Owner))
            RaiseLocalEvent(item, ref evHeld);

        Dirty(ent);

        _movementSystem.RefreshMovementSpeedModifiers(ent);
    }
}

[ByRefEvent]
public readonly record struct ParametersChangedEvent()
{
}

[ByRefEvent]
public record struct ParametersUpdateEvent() : IInventoryRelayEvent
{
    public Dictionary<Parameter, FixedPoint2> ModifiedValues = new Dictionary<Parameter, FixedPoint2>();
    SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;
}

[ByRefEvent]
public readonly record struct UserParametersChangedEvent(EntityUid User);
