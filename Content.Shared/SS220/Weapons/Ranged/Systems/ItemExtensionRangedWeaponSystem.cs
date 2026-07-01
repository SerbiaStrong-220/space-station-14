// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.SS220.ItemExtension;
using Content.Shared.SS220.PhysicalParameters;
using Content.Shared.SS220.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using static Content.Shared.SS220.PhysicalParameters.PhysicalParametersSystem;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed class ItemExtensionRangedWeaponSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private PhysicalParametersSystem _parameters = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemExtensionRangedWeaponComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<ItemExtensionRangedWeaponComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<ItemExtensionRangedWeaponComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<ItemExtensionRangedWeaponComponent, UserParametersChangedEvent>(OnUserParametersChanged);
    }

    private void OnEquip(Entity<ItemExtensionRangedWeaponComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.User = args.User;
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnUnequip(Entity<ItemExtensionRangedWeaponComponent> ent, ref GotUnequippedHandEvent args)
    {
        ent.Comp.User = null;
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnUserParametersChanged(Entity<ItemExtensionRangedWeaponComponent> ent, ref UserParametersChangedEvent args)
    {
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnGunRefreshModifiers(Entity<ItemExtensionRangedWeaponComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (ent.Comp.User is not { Valid: true } userValidated)
            return;

        if (!TryComp<PhysicalParametersComponent>(userValidated, out var userParametersComp))
            return;

        if (!TryComp<ItemExtensionComponent>(ent, out var extensionComp))
            return;

        FixedPoint2 toDivide = extensionComp.StrengthRequirementToBeUsed - extensionComp.MinimalStrengthToPickUp;

        if (toDivide == 0)
            toDivide = 1;

        FixedPoint2 strengthModifier = (_parameters.GetParameterValue((userValidated, userParametersComp), Parameter.Strength) - extensionComp.MinimalStrengthToPickUp) / toDivide;

        args.MinAngle += (ent.Comp.MinAngle.Theta * strengthModifier).Float();
        args.MaxAngle += (ent.Comp.MaxAngle.Theta * strengthModifier).Float();
        args.AngleDecay += (ent.Comp.AngleDecay.Theta * strengthModifier).Float();
        args.AngleIncrease += (ent.Comp.AngleIncrease.Theta * strengthModifier).Float();
    }
}
