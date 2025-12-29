using Content.Shared.Blocking;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.SS220.ItemToggle;

public sealed class ItemToggleBlockingDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemToggleBlockingDamageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ItemToggleBlockingDamageComponent, ItemToggledEvent>(OnToggleItem);
    }

    private void OnDecreaseBlock(Entity<ItemToggleBlockingDamageComponent> ent, BlockingComponent blockingComponent)
    {
        // if (ent.Comp.DeactivatedPassiveModifier != null)
        //     blockingComponent.PassiveBlockDamageModifer = ent.Comp.DeactivatedPassiveModifier;
        // if (ent.Comp.DeactivatedActiveModifier != null)
        //     blockingComponent.ActiveBlockDamageModifier = ent.Comp.DeactivatedActiveModifier;
        ent.Comp.IsToggled = false;

        blockingComponent.RangeBlockProb = ent.Comp.BaseRangeBlockProb;
        blockingComponent.MeleeBlockProb = ent.Comp.BaseMeleeBlockProb;

        Dirty(ent);
    }

    private void OnMapInit(Entity<ItemToggleBlockingDamageComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<BlockingComponent>(ent.Owner, out var blockingComponent))
        {
            return;
        }
        OnDecreaseBlock(ent, blockingComponent);
    }

    private void OnToggleItem(Entity<ItemToggleBlockingDamageComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<BlockingComponent>(ent.Owner, out var blockingComponent))
            return;

        if (args.Activated)
        {
            //if (ent.Comp.OriginalPassiveModifier != null)
            //    blockingComponent.PassiveBlockDamageModifer = ent.Comp.OriginalPassiveModifier;
            //if (ent.Comp.OriginalActiveModifier != null)
            //    blockingComponent.ActiveBlockDamageModifier = ent.Comp.OriginalActiveModifier;
            ent.Comp.IsToggled = true;

            blockingComponent.RangeBlockProb = ent.Comp.ToggledRangeBlockProb;
            blockingComponent.MeleeBlockProb = ent.Comp.ToggledMeleeBlockProb;

            Dirty(ent);
        }
        else
        {
            OnDecreaseBlock(ent, blockingComponent);
        }
    }
}
