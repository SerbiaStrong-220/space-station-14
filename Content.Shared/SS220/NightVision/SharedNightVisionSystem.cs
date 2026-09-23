using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.SS220.NightVision;

public abstract partial class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<NightVisionComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NightVisionComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<NightVisionComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<NightVisionComponent, ImplantRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<NightVisionComponent, ToggleNightVisionEvent>(OnToggle);
    }

    private void OnStartup(Entity<NightVisionComponent> ent, ref ComponentStartup args)
    {
        // they could turn nightvision itself
        if (HasComp<ClothingComponent>(ent) || HasComp<SubdermalImplantComponent>(ent))
            return;

        GrantVision(ent, ent.Owner);
    }

    private void OnShutdown(Entity<NightVisionComponent> ent, ref ComponentShutdown args)
    {
        RevokeVision(ent);
    }

    private void OnEquipped(Entity<NightVisionComponent> ent, ref GotEquippedEvent args)
    {
        if ((ent.Comp.Slots & args.SlotFlags) != 0)
            GrantVision(ent, args.EquipTarget);
    }

    private void OnUnequipped(Entity<NightVisionComponent> ent, ref GotUnequippedEvent args)
    {
        if ((ent.Comp.Slots & args.SlotFlags) != 0)
            RevokeVision(ent);
    }

    private void OnImplanted(Entity<NightVisionComponent> ent, ref ImplantImplantedEvent args)
    {
        GrantVision(ent, args.Implanted);
    }

    private void OnRemoved(Entity<NightVisionComponent> ent, ref ImplantRemovedEvent args)
    {
        RevokeVision(ent);
    }

    private void GrantVision(Entity<NightVisionComponent> ent, EntityUid wearer)
    {
        ent.Comp.Wearer = wearer;
        if (ent.Comp.Action != null)
        {
            _actions.AddAction(wearer, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
            _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Enabled);
        }

        Dirty(ent);
    }

    private void RevokeVision(Entity<NightVisionComponent> ent)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);

        ent.Comp.Wearer = null;
        ent.Comp.Enabled = false;

        _actions.SetToggled(ent.Comp.ActionEntity, false);
        Dirty(ent);
    }

    private void OnToggle(Entity<NightVisionComponent> ent, ref ToggleNightVisionEvent args)
    {
        if (args.Handled || ent.Comp.Wearer != args.Performer)
            return;

        SetEnabled(ent, !ent.Comp.Enabled);
        args.Handled = true;
    }

    public void SetEnabled(Entity<NightVisionComponent> ent, bool enabled)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;

        _actions.SetToggled(ent.Comp.ActionEntity, enabled);
        Dirty(ent);
    }
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent;
