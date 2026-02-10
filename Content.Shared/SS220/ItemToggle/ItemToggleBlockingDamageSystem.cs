using Content.Shared.Blocking;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.SS220.AltBlocking;
using Content.Shared.SS220.ChangeAppearanceOnActiveBlocking;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.ItemToggle;

public sealed class ItemToggleBlockingDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ItemToggleBlockingDamageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ItemToggleBlockingDamageComponent, ItemToggledEvent>(OnToggleItem);
    }

    private void OnDecreaseBlock(Entity<ItemToggleBlockingDamageComponent> ent, AltBlockingComponent AltBlockingComponent)
    {
        if (TryComp<ChangeAppearanceOnActiveBlockingComponent>(ent.Owner, out var appearanceComp))
        {
            var ev = new ActiveBlockingEvent(false);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
        ent.Comp.IsToggled = false;

        AltBlockingComponent.RangeBlockProb = ent.Comp.BaseRangeBlockProb;
        AltBlockingComponent.MeleeBlockProb = ent.Comp.BaseMeleeBlockProb;

        Dirty(ent);
    }

    private void OnMapInit(Entity<ItemToggleBlockingDamageComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<AltBlockingComponent>(ent.Owner, out var AltBlockingComponent))
            return;

        OnDecreaseBlock(ent, AltBlockingComponent);
    }

    private void OnToggleItem(Entity<ItemToggleBlockingDamageComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<AltBlockingComponent>(ent.Owner, out var AltBlockingComponent))
            return;

        if (!TryComp<AltBlockingUserComponent>(AltBlockingComponent.User, out var userComp))
            return;


        if (args.Activated)
        {
            ent.Comp.IsToggled = true;

            AltBlockingComponent.RangeBlockProb = ent.Comp.ToggledRangeBlockProb;
            AltBlockingComponent.MeleeBlockProb = ent.Comp.ToggledMeleeBlockProb;

            Dirty(ent);

            if (TryComp<ChangeAppearanceOnActiveBlockingComponent>(ent.Owner, out var appearanceComp) && userComp.IsBlocking)
            {
                var ev = new ActiveBlockingEvent(true);
                RaiseLocalEvent(ent.Owner,ref ev);
            }
            return;
        }
        OnDecreaseBlock(ent, AltBlockingComponent);
    }
}
