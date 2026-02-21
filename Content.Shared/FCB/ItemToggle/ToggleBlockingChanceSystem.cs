// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.FCB.AltBlocking;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.FCB.AltBlocking;
using Content.Shared.FCB.ChangeAppearanceOnActiveBlocking;

namespace Content.Shared.FCB.ToggleBlocking;

public sealed class ToggleBlockingChanceSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ToggleBlockingChanceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ToggleBlockingChanceComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnMapInit(Entity<ToggleBlockingChanceComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<AltBlockingComponent>(ent.Owner, out var blockingComponent))
            return;

        ActivateBlock(ent, blockingComponent);
    }

    private void OnToggled(Entity<ToggleBlockingChanceComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<AltBlockingComponent>(ent.Owner, out var blockingComponent))
            return;

        if (!TryComp<AltBlockingUserComponent>(blockingComponent.User, out var userComp))
            return;


        if (args.Activated)
        {
            ActivateBlock(ent, blockingComponent);
            Dirty(ent, blockingComponent);
            return;
        }

        DectivateBlock(ent, blockingComponent);
        Dirty(ent, blockingComponent);
    }

    private void DectivateBlock(Entity<ToggleBlockingChanceComponent> ent, AltBlockingComponent blockingComponent)
    {
        if (TryComp<ChangeAppearanceOnActiveBlockingComponent>(ent.Owner, out var appearanceComp))
        {
            var ev = new ActiveBlockingEvent(false);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
        ent.Comp.IsToggled = false;

        blockingComponent.RangeBlockProb = ent.Comp.BaseRangeBlockProb;
        blockingComponent.MeleeBlockProb = ent.Comp.BaseMeleeBlockProb;
    }

    private void ActivateBlock(Entity<ToggleBlockingChanceComponent> ent, AltBlockingComponent blockingComponent)
    {
        if (TryComp<ChangeAppearanceOnActiveBlockingComponent>(ent.Owner, out var appearanceComp))
        {
            var ev = new ActiveBlockingEvent(true);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
        ent.Comp.IsToggled = true;

        blockingComponent.RangeBlockProb = ent.Comp.ToggledRangeBlockProb;
        blockingComponent.MeleeBlockProb = ent.Comp.ToggledMeleeBlockProb;
    }
}
