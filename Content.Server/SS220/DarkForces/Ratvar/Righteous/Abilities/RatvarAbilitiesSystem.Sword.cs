using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment.Weapons;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
//using Content.Shared.SS220.DarkForces.Ratvar.UI;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities;

public sealed partial class RatvarAbilitiesSystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

    private const string SwordsmanEnchantment = "RatvarSwordsSwordsman";
    private const string BloodshedEnchantment = "RatvarSwordBloodshed";

    private void InitializeSword()
    {
        SubscribeLocalEvent<RatvarSwordComponent, MeleeHitEvent>(OnSwordMeleeHit);
        SubscribeLocalEvent<RatvarSwordComponent, GetMeleeAttackRateEvent>(OnGetMeleeAttackRate);
    }

    private void OnGetMeleeAttackRate(EntityUid uid, RatvarSwordComponent component, GetMeleeAttackRateEvent args)
    {
        if (!IsEnchantmentActive(args.Weapon, out var enchantment) || enchantment != SwordsmanEnchantment)
            return;

        args.Multipliers += 1.5f;
    }

    private void OnSwordMeleeHit(EntityUid uid, RatvarSwordComponent component, MeleeHitEvent args)
    {
        if (!IsEnchantmentActive(args.Weapon, out var enchantment))
            return;

        switch (enchantment)
        {
            case BloodshedEnchantment:
            {
                foreach (var player in args.HitEntities)
                {
                    if (!HasComp<BloodstreamComponent>(player))
                        continue;

                    _bloodstreamSystem.TryModifyBleedAmount(player, 100);
                }

                break;
            }
            case SwordsmanEnchantment:
                args.BonusDamage = component.SwordsmanDamage;
                break;
        }
    }
}
