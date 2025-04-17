using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Toggleable;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Handles adding and using a toggle action for <see cref="ToggleClothingComponent"/>.
/// </summary>
public sealed class InnerToggleableClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerToggleableClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InnerToggleableClothingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<InnerToggleableClothingComponent, InnerToggleableActionEvent>(OnToggleAction);
        SubscribeLocalEvent<InnerToggleableClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnMapInit(Entity<InnerToggleableClothingComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        // test funny
        if (string.IsNullOrEmpty(comp.Action))
            return;

        _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
        //_actions.SetToggled(comp.ActionEntity, _toggle.IsActivated(ent.Owner));
        Dirty(uid, comp);
    }

    private void OnGetActions(Entity<InnerToggleableClothingComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands && ent.Comp.MustEquip)
            return;

        var ev = new ToggleClothingCheckEvent(args.User);
        RaiseLocalEvent(ent, ref ev);

        if (!ev.Cancelled)
            args.AddAction(ent.Comp.ActionEntity);
    }

    private void OnToggleAction(Entity<InnerToggleableClothingComponent> ent, ref InnerToggleableActionEvent args)
    {

    }

    private void OnUnequipped(Entity<InnerToggleableClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {

    }
}
