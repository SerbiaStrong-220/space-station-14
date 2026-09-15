using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Clothing;
using Content.Shared.Toggleable;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.NightVision;

public abstract class SharedNightVisionSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<NightVisionComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<NightVisionComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<NightVisionComponent, ToggleActionEvent>(OnToggled);
    }

    private void OnCompInit(Entity<NightVisionComponent> ent, ref ComponentInit _)
    {
        if (!HasComp<ActionsContainerComponent>(ent))
            return;

        if (!_actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner))
            return;

        if (!ent.Comp.Enabled)
            return;

        EnableOverlay(ent);
    }

    private void OnShutdown(Entity<NightVisionComponent> ent, ref ComponentShutdown _)
    {
        if (!HasComp<ActionsContainerComponent>(ent))
            return;

        DisableOverlay();
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnGotEquipped(Entity<NightVisionComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!GameTiming.IsFirstTimePredicted)
            return;

        ent.Comp.Enabled = false;

        if (!_actions.AddAction(args.Wearer, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner))
            return;

        Dirty(ent);
    }

    private void OnGotUnequipped(Entity<NightVisionComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!GameTiming.IsFirstTimePredicted)
            return;

        DisableOverlay();
        _actions.RemoveProvidedActions(args.Wearer, ent.Owner);
        ent.Comp.Enabled = false;
        Dirty(ent);
    }

    private void OnToggled(Entity<NightVisionComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!GameTiming.IsFirstTimePredicted)
            return;

        if (ent.Comp.Enabled)
        {
            DisableOverlay();
            SetActivated(ent, false);
        }
        else
        {
            EnableOverlay(ent);
            SetActivated(ent, true);
        }

        args.Handled = true;
        Dirty(ent);
    }

    private void SetActivated(Entity<NightVisionComponent> ent, bool activated)
    {
        if (ent.Comp.Enabled == activated)
            return;

        ent.Comp.Enabled = activated;
        Dirty(ent);
    }

    protected virtual void EnableOverlay(Entity<NightVisionComponent> ent) { }

    protected virtual void DisableOverlay() { }
}
