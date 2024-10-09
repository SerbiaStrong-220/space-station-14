using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment.Weapons;
using Content.Shared.Humanoid;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities;

public sealed partial class RatvarAbilitiesSystem
{
    private const string ElectricalTouchEnchantment = "RatvarSpearElectricalTouch";

    private void InitializeSpear()
    {
        SubscribeLocalEvent<RatvarSpearComponent, MeleeHitEvent>(OnSpearMeleeHit);
    }

    private void OnSpearMeleeHit(EntityUid uid, RatvarSpearComponent component, MeleeHitEvent args)
    {
        if (!IsWeaponWieldable(args.Weapon) || !IsEnchantmentActive(args.Weapon, out var enchantment))
            return;

        if (enchantment == ElectricalTouchEnchantment)
        {
            DoEMP(args.HitEntities);
        }
    }

    private void DoEMP(IReadOnlyList<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            if (HasComp<HumanoidAppearanceComponent>(entity))
            {
                var transform = Transform(entity).MapPosition;
                _empSystem.EmpPulse(transform, 0.1f, 15000, 3);
            }

            if (HasComp<BorgChassisComponent>(entity))
            {
                var transform = Transform(entity).MapPosition;
                _empSystem.EmpPulse(transform, 0.1f, 25000, 7);
            }
        }
    }
}
