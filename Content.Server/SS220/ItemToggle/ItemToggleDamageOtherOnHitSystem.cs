using Content.Server.Damage.Components;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server.SS220.ItemToggle;

public sealed class ItemToggleDamageOtherOnHitSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemToggleDamageOtherOnHitComponent, ItemToggledEvent>(OnToggleItem);
    }

    private void OnToggleItem(EntityUid uid, ItemToggleDamageOtherOnHitComponent component, ItemToggledEvent args)
    {
        if (!TryComp<DamageOtherOnHitComponent>(uid, out var damageOtherOnHit))
            return;

        if (args.Activated)
        {
            if (component.ActivatedDamage != null)
            {
                //Setting deactivated damage to the weapon's regular value before changing it.
                component.DeactivatedDamage ??= damageOtherOnHit.Damage;
                damageOtherOnHit.Damage = component.ActivatedDamage;
            }
        }
        else
        {
            if (component.DeactivatedDamage != null)
                damageOtherOnHit.Damage = component.DeactivatedDamage;
        }
    }
}
