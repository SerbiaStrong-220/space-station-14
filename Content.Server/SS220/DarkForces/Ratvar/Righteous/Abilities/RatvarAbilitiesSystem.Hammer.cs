using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment.Weapons;
using Content.Shared.Humanoid;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics.Components;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities;

public sealed partial class RatvarAbilitiesSystem
{
    private const string KnockOffEnchantment = "RatvarHammmerKnockOff";

    private void InitializeHammer()
    {
        SubscribeLocalEvent<RatvarHammerComponent, MeleeHitEvent>(OnHammerMeleeHit);
    }

    private void OnHammerMeleeHit(EntityUid uid, RatvarHammerComponent component, MeleeHitEvent args)
    {
        if (!IsWeaponWieldable(args.Weapon) || !IsEnchantmentActive(args.Weapon, out var enchantment))
            return;

        if (enchantment == KnockOffEnchantment)
        {
            foreach (var player in args.HitEntities)
            {
                if (!HasComp<PhysicsComponent>(player) || !HasComp<HumanoidAppearanceComponent>(player))
                    return;

                var fieldDir = _transformSystem.GetWorldPosition(uid);
                var playerDir = _transformSystem.GetWorldPosition(player);

                _throwing.TryThrow(player, playerDir - fieldDir, 50);
            }
        }
    }
}
