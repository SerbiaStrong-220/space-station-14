using Content.Shared.Damage.Components;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.SS220.ItemToggle;

public sealed class ItemToggleDamageOtherOnHitSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemToggleDamageOtherOnHitComponent, ItemToggledEvent>(OnToggleItem);
    }

    private void OnToggleItem(Entity<ItemToggleDamageOtherOnHitComponent> entity, ref ItemToggledEvent args)
    {
        if (!TryComp<DamageOtherOnHitComponent>(entity, out var damageOtherOnHit))
            return;

        if (args.Activated)
        {
            if (entity.Comp.ActivatedDamage == null)
                return;

            //Setting deactivated damage to the weapon's regular value before changing it.
            entity.Comp.DeactivatedDamage ??= damageOtherOnHit.Damage;
            damageOtherOnHit.Damage = entity.Comp.ActivatedDamage;

            Dirty(entity);
        }
        else
        {
            if (entity.Comp.DeactivatedDamage != null)
            {
                damageOtherOnHit.Damage = entity.Comp.DeactivatedDamage;
                Dirty(entity);
            }
        }
    }
}
