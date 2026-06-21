// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.SS220.PhysicalParameters;

public sealed class PhysicalParametersSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PhysicalParametersComponent, MeleeAttackerEvent>(OnMeleeAttack);

        base.Initialize();
    }

    public void OnMeleeAttack(Entity<PhysicalParametersComponent> ent, ref MeleeAttackerEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(args.Used, out var weaponComp) ||
            !weaponComp.StrengthAffectsDamage)
            return;

        FixedPoint2 strengthModifier = 1f;

        if (ent.Comp.StrengthAffectsArms &&
            ent.Comp.ParameterDict.ContainsKey(Parameter.Strength))
            strengthModifier = ent.Comp.ParameterDict[Parameter.Strength];

        if (TryComp<HandsComponent>(ent.Owner, out var handsComp) &&
            handsComp.ActiveHandId != null &&
            _handsSystem.TryGetHand(ent.Owner, handsComp.ActiveHandId, out var activeHand) &&
            activeHand.Value.StrengthModifier != null)
            strengthModifier = (FixedPoint2)activeHand.Value.StrengthModifier;

        FixedPoint2 itemReqModifier = 1f;

        if (TryComp<ItemExtensionComponent>(args.Used, out var extensionComp))
            strengthModifier = (strengthModifier - extensionComp.MinimalStrengthToPickUp) / extensionComp.StrengthRequirementToBeUsed - 1;

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

    public void AddParameter(Entity<PhysicalParametersComponent> ent, Parameter parameter, FixedPoint2 value)
    {
        if (ent.Comp.ParameterDict.ContainsKey(parameter))
            ent.Comp.ParameterDict[parameter] += value;
    }

    public void SetParameter(Entity<PhysicalParametersComponent> ent, Parameter parameter, FixedPoint2 value)
    {
        if (ent.Comp.ParameterDict.ContainsKey(parameter))
            ent.Comp.ParameterDict[parameter] = value;
    }
}
