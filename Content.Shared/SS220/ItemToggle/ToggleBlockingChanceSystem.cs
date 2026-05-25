// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.SS220.AltBlocking;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.SS220.ChangeAppearanceOnActiveBlocking;

namespace Content.Shared.SS220.ToggleBlocking;

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

        DeactivateBlock(ent, blockingComponent);
    }

    private void OnToggled(Entity<ToggleBlockingChanceComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<AltBlockingComponent>(ent.Owner, out var blockingComponent))
            return;

        if (!TryComp<AltBlockingUserComponent>(blockingComponent.User, out var userComp))
            return;

        if (TryComp<ChangeAppearanceOnActiveBlockingComponent>(ent.Owner, out var appearanceComp))
        {
            var ev = new ActiveBlockingStateChanged(userComp.Blocking && args.Activated);
            RaiseLocalEvent(ent.Owner, ref ev);
        }

        if (args.Activated)
        {
            ActivateBlock(ent, blockingComponent);
            Dirty(ent.Owner, blockingComponent);
            Dirty(ent);
            return;
        }

        DeactivateBlock(ent, blockingComponent);
        Dirty(ent.Owner, blockingComponent);
        Dirty(ent);
    }

    private void DeactivateBlock(Entity<ToggleBlockingChanceComponent> ent, AltBlockingComponent blockingComponent)
    {
        ent.Comp.Toggled = false;

        blockingComponent.RangeBlockProb = ent.Comp.BaseRangeBlockProb;
        blockingComponent.MeleeBlockProb = ent.Comp.BaseMeleeBlockProb;
    }

    private void ActivateBlock(Entity<ToggleBlockingChanceComponent> ent, AltBlockingComponent blockingComponent)
    {
        ent.Comp.Toggled = true;

        blockingComponent.RangeBlockProb = ent.Comp.ToggledRangeBlockProb;
        blockingComponent.MeleeBlockProb = ent.Comp.ToggledMeleeBlockProb;
    }
}
