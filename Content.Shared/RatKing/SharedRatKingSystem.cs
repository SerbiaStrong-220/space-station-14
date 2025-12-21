using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.RatKing.Components;
using Content.Shared.SS220.RatKing;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;


namespace Content.Shared.RatKing;

public abstract class SharedRatKingSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!; //SS220 RatKing tweaks and changes
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!; // SS220 Ratking tweaks
    [Dependency] private readonly TagSystem _tagSystem = default!; // make ratking rats Trash after death

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RatKingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RatKingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RatKingComponent, RatKingOrderActionEvent>(OnOrderAction);
        SubscribeLocalEvent<RatKingServantComponent, ComponentShutdown>(OnServantShutdown);
        SubscribeLocalEvent<RatKingComponent, RatKingRummageActionEvent>(OnRummageAction); //SS220 RatKing Tweaks and Changes
    }

    private void OnStartup(EntityUid uid, RatKingComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        _action.AddAction(uid, ref component.ActionRaiseArmyEntity, component.ActionRaiseArmy, component: comp);
        _action.AddAction(uid, ref component.ActionDomainEntity, component.ActionDomain, component: comp);
        _action.AddAction(uid, ref component.ActionOrderStayEntity, component.ActionOrderStay, component: comp);
        _action.AddAction(uid, ref component.ActionOrderFollowEntity, component.ActionOrderFollow, component: comp);
        _action.AddAction(uid, ref component.ActionOrderCheeseEmEntity, component.ActionOrderCheeseEm, component: comp);
        _action.AddAction(uid, ref component.ActionOrderLooseEntity, component.ActionOrderLoose, component: comp);
        _action.AddAction(uid, ref component.ActionRummageEntity, component.ActionRummage, component: comp); //SS220 RatKing Tweaks and Changes

        UpdateActions(uid, component);
    }

    private void OnShutdown(EntityUid uid, RatKingComponent component, ComponentShutdown args)
    {
        foreach (var servant in component.Servants)
        {
            if (TryComp(servant, out RatKingServantComponent? servantComp))
                servantComp.King = null;
        }

        if (!TryComp(uid, out ActionsComponent? comp))
            return;

        var actions = new Entity<ActionsComponent?>(uid, comp);
        _action.RemoveAction(actions, component.ActionRaiseArmyEntity);
        _action.RemoveAction(actions, component.ActionDomainEntity);
        _action.RemoveAction(actions, component.ActionOrderStayEntity);
        _action.RemoveAction(actions, component.ActionOrderFollowEntity);
        _action.RemoveAction(actions, component.ActionOrderCheeseEmEntity);
        _action.RemoveAction(actions, component.ActionOrderLooseEntity);
        _action.RemoveAction(actions, component.ActionRummageEntity); //SS220 RatKing Tweaks and Changes
    }

    private void OnOrderAction(EntityUid uid, RatKingComponent component, RatKingOrderActionEvent args)
    {
        if (component.CurrentOrder == args.Type)
            return;
        args.Handled = true;

        component.CurrentOrder = args.Type;
        Dirty(uid, component);

        DoCommandCallout(uid, component);
        UpdateActions(uid, component);
        UpdateAllServants(uid, component);
    }

    private void OnServantShutdown(EntityUid uid, RatKingServantComponent component, ComponentShutdown args)
    {
        if (TryComp(component.King, out RatKingComponent? ratKingComponent))
            ratKingComponent.Servants.Remove(uid);
    }

    //ss220 rat servant fix begin
    private void OnServantDie(EntityUid uid, RatKingServantComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        EnsureComp<ItemComponent>(uid);

        _tagSystem.AddTag(uid, "Trash");
    }
    //ss220 rat servant fix end

    private void UpdateActions(EntityUid uid, RatKingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _action.SetToggled(component.ActionOrderStayEntity, component.CurrentOrder == RatKingOrderType.Stay);
        _action.SetToggled(component.ActionOrderFollowEntity, component.CurrentOrder == RatKingOrderType.Follow);
        _action.SetToggled(component.ActionOrderCheeseEmEntity, component.CurrentOrder == RatKingOrderType.CheeseEm);
        _action.SetToggled(component.ActionOrderLooseEntity, component.CurrentOrder == RatKingOrderType.Loose);
        _action.StartUseDelay(component.ActionOrderStayEntity);
        _action.StartUseDelay(component.ActionOrderFollowEntity);
        _action.StartUseDelay(component.ActionOrderCheeseEmEntity);
        _action.StartUseDelay(component.ActionOrderLooseEntity);
    }
    //SS220 RatKing Tweaks and Changes start
    private void OnRummageAction(Entity<RatKingComponent> entity, ref RatKingRummageActionEvent args)
    {
        if (args.Handled || !TryComp<RummageableComponent>(args.Target, out var rumComp))
        {
            _popup.PopupPredicted(Loc.GetString("ratking-rummage-failure"), args.Target, entity, PopupType.Small);
            return;
        }

        if (rumComp.Looted)
        {
            _popup.PopupPredicted(Loc.GetString("ratking-rummage-looted-failure"), args.Target, entity, PopupType.Small);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, entity, rumComp.RummageDuration,
            new RatKingRummageDoAfterEvent(), args.Target, args.Target)
        {
            BlockDuplicate = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2f
        };
        _popup.PopupPredicted(Loc.GetString("ratking-rummage-success"), args.Target, entity, PopupType.Small);
        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }
    //SS220 RatKing Tweaks and Changes end

    public void UpdateAllServants(EntityUid uid, RatKingComponent component)
    {
        foreach (var servant in component.Servants)
        {
            UpdateServantNpc(servant, component.CurrentOrder);
        }
    }

    public virtual void UpdateServantNpc(EntityUid uid, RatKingOrderType orderType)
    {

    }

    public virtual void DoCommandCallout(EntityUid uid, RatKingComponent component)
    {

    }
}

[Serializable, NetSerializable]
public sealed partial class RatKingRummageDoAfterEvent : SimpleDoAfterEvent
{

}
