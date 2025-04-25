using Content.Shared.Actions;
using Content.Shared.Blocking;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// </summary>
public sealed class InnerHandToggleProviderSystemSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerHandToggleProviderSystemComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<InnerHandToggleProviderSystemComponent, GotUnequippedHandEvent>(OnUnequip);
        //SubscribeLocalEvent<InnerToggleableClothingComponent, DroppedEvent>(OnDrop);
    }

    private void OnEquip(Entity<InnerToggleableClothingComponent> ent, ref GotEquippedHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        //var innerUser = EnsureComp<InnerToggleableComponent>(args.Equipee);
    }

    private void OnUnequip(Entity<InnerToggleableClothingComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

    }

    /*
    private void OnDrop(Entity<InnerToggleableClothingComponent> ent, ref DroppedEvent args)
    {

    }
    */
}
