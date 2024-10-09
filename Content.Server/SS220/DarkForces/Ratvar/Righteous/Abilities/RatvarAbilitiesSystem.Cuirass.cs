using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment.Armor;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.Armor;
using RatvarEnchantmentableComponent = Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.RatvarEnchantmentableComponent;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities;

public sealed partial class RatvarAbilitiesSystem
{
    private const int MaxAbsorbCount = 2;

    private void InitializeCuirass()
    {
        SubscribeLocalEvent<RatvarCuirassComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnCuirassDamageModify);
        SubscribeLocalEvent<RatvarEnchantmentableComponent, RatvarCuirassAbsorbEvent>(OnAbsorbEvent);
    }

    private void OnAbsorbEvent(EntityUid uid, RatvarEnchantmentableComponent component, RatvarCuirassAbsorbEvent args)
    {
        component.IsEnchantmentActive = true;
        component.DisableAbilityTick = _gameTiming.CurTime + TimeSpan.FromSeconds(8);
    }

    private void OnCuirassDamageModify(EntityUid uid, RatvarCuirassComponent component,
        InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (component.IsAbsorb)
        {
            if (component.AbsorbCount >= MaxAbsorbCount)
            {
                component.IsAbsorb = false;
            }
        }

        if (component is { IsAbsorb: true, AbsorbCount: < MaxAbsorbCount })
        {
            args.Args.Damage *= 0;
            component.AbsorbCount++;
            return;
        }
    }
}
