using Content.Shared.Blocking;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server.SS220.ItemToggle;

public sealed class ItemToggleBlockingDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemToggleBlockingDamageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ItemToggleBlockingDamageComponent, ItemToggledEvent>(OnToggleItem);
    }

    private void OnDecreaseBlock(Entity<ItemToggleBlockingDamageComponent> ent, BlockingComponent blockingComponent)
    {
        if (ent.Comp.DeactivatedPassiveModifier != null)
            blockingComponent.PassiveBlockDamageModifer = ent.Comp.DeactivatedPassiveModifier;
        if (ent.Comp.DeactivatedActiveModifier != null)
            blockingComponent.ActiveBlockDamageModifier = ent.Comp.DeactivatedActiveModifier;

        blockingComponent.ActiveBlockFraction = ent.Comp.DeactivatedActiveFraction;
        blockingComponent.PassiveBlockFraction = ent.Comp.DeactivatedPassiveFraction;
    }

    private void OnMapInit(Entity<ItemToggleBlockingDamageComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<BlockingComponent>(ent.Owner, out var blockingComponent))
        {
            return;
        }

        ent.Comp.OriginalActiveModifier = blockingComponent.ActiveBlockDamageModifier;
        ent.Comp.OriginalPassiveModifier = blockingComponent.PassiveBlockDamageModifer;
        ent.Comp.OriginalActivatedFraction = blockingComponent.ActiveBlockFraction;
        ent.Comp.OriginalDeactivatedFraction = blockingComponent.PassiveBlockFraction;

        OnDecreaseBlock(ent, blockingComponent);
    }

    private void OnToggleItem(Entity<ItemToggleBlockingDamageComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<BlockingComponent>(ent.Owner, out var blockingComponent))
            return;

        if (args.Activated)
        {
            if (ent.Comp.OriginalPassiveModifier != null)
                blockingComponent.PassiveBlockDamageModifer = ent.Comp.OriginalPassiveModifier;
            if (ent.Comp.OriginalActiveModifier != null)
                blockingComponent.ActiveBlockDamageModifier = ent.Comp.OriginalActiveModifier;

            blockingComponent.ActiveBlockFraction = ent.Comp.OriginalActivatedFraction;
            blockingComponent.PassiveBlockFraction = ent.Comp.OriginalDeactivatedFraction;

        }
        else
        {
            OnDecreaseBlock(ent, blockingComponent);
        }
    }
}
