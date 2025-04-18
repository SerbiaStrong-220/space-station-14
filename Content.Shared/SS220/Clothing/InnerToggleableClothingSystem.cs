using Content.Shared.Actions;
using Content.Shared.Blocking;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// Handles adding and using a toggle action for <see cref="ToggleClothingComponent"/>.
/// </summary>
public sealed class InnerToggleableClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerToggleableClothingComponent, GotEquippedEvent>(OnInnerToggleableEquip);

        SubscribeLocalEvent<InnerToggleableClothingComponent, GotUnequippedEvent>(OnInnerToggleableUnequip);

        SubscribeLocalEvent<InnerToggleableClothingComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<InnerToggleableClothingComponent, GotUnequippedHandEvent>(OnUnequip);
        //SubscribeLocalEvent<InnerToggleableClothingComponent, DroppedEvent>(OnDrop);
    }

    private void OnInnerToggleableEquip(Entity<InnerToggleableClothingComponent> ent, ref GotEquippedEvent args)
    {
        //var innerUser = EnsureComp<InnerToggleableComponent>(args.Equipee);
    }

    private void OnInnerToggleableUnequip(Entity<InnerToggleableClothingComponent> ent, ref GotUnequippedEvent args)
    {

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
