// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.StuckOnEquip;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.InnerHandToggleable;

public sealed class SharedInnerHandToggleableSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hand = default!;
    [Dependency] private readonly SharedStuckOnEquipSystem _stuckOnEquip = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public const string InnerHandPrefix = "inner_";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerHandToggleableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InnerHandToggleableComponent, DidEquipHandEvent>(OnDidEquipHand);
        SubscribeLocalEvent<InnerHandToggleableComponent, DidUnequipHandEvent>(OnDidUnequipHand);
        SubscribeLocalEvent<InnerHandToggleableComponent, DidSwitchHandEvent>(OnDidSwitchHand);
        SubscribeLocalEvent<InnerHandToggleableComponent, ToggleInnerHandEvent>(OnToggleInnerHand);
        SubscribeLocalEvent<InnerHandToggleableComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnMapInit(Entity<InnerHandToggleableComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HandsComponent>(ent, out var handsComp))
            return;

        var manager = EnsureComp<ContainerManagerComponent>(ent);
        int unusedPrefix = SharedBodySystem.PartSlotContainerIdPrefix.Length;

        //pre-creating everything required
        foreach (var hand in handsComp.SortedHands)
        {
            var name = string.Concat(InnerHandPrefix, hand.AsSpan(unusedPrefix)); ;//add and delete shit
            var handInfo = new InnerContainerInfo
            {
                Container = _containerSystem.EnsureContainer<ContainerSlot>(ent, name, manager),
                ContainerId = name
            };

            ent.Comp.HandsContainers.Add(hand, handInfo);
        }

        if (!_actionContainer.EnsureAction(ent, ref ent.Comp.ActionEntity, out _, ent.Comp.Action))
            return;

        Dirty(ent, ent.Comp);
    }

    private void OnComponentShutdown(Entity<InnerHandToggleableComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Comp.ActionEntity);
        foreach (var hand in ent.Comp.HandsContainers.Values)
        {
            if (hand.Container is null)
                return;

            _containerSystem.EmptyContainer(hand.Container);
            _containerSystem.ShutdownContainer(hand.Container);
        }
        Dirty(ent);
    }

    private void OnDidEquipHand(Entity<InnerHandToggleableComponent> ent, ref DidEquipHandEvent args)
    {
        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Equipped))
            return;

        if (!ent.Comp.HandsContainers.TryGetValue(args.Hand.Name, out var innerToggle))
            return;

        if (innerToggle.InnerItemUid != null)//if we don't have space for a new item, we don't update the action
            return;

        UpdateToggleAction(ent, args.Equipped, false);
    }

    private void OnDidUnequipHand(Entity<InnerHandToggleableComponent> ent, ref DidUnequipHandEvent args)
    {
        if (!TryComp<HandsComponent>(ent, out var handsComp))
            return;

        if (args.Hand != handsComp.ActiveHand)//if the item was lost not from the ActiveHand, cause action matters only in it
            return;

        if (!ent.Comp.HandsContainers.TryGetValue(handsComp.ActiveHand.Name, out var innerToggle))
            return;

        if (innerToggle.InnerItemUid != null)//if the inner item is inside
            return;

        _actionsSystem.RemoveAction(ent.Comp.ActionEntity);
    }

    private void OnDidSwitchHand(Entity<InnerHandToggleableComponent> ent, ref DidSwitchHandEvent args)
    {
        if (!TryComp<HandsComponent>(ent, out var handsComp))
            return;

        if (handsComp.ActiveHand == null)
            return;

        if (!ent.Comp.HandsContainers.TryGetValue(handsComp.ActiveHand.Name, out var innerToggle))
            return;

        if (innerToggle.InnerItemUid != null)//if there is an item inside, then the action gets its icon
        {
            UpdateToggleAction(ent, innerToggle.InnerItemUid.Value, true);
            return;
        }

        //if there is an object in the hand, there is nothing inside and it fits -- we update the action
        if (handsComp.ActiveHand.HeldEntity != null && _whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, handsComp.ActiveHand.HeldEntity.Value))
        {
            UpdateToggleAction(ent, handsComp.ActiveHand.HeldEntity.Value, false);
            return;
        }

        _actionsSystem.RemoveAction(ent.Comp.ActionEntity);
    }

    /// <summary>
    /// Updates visual of the action based on parameters
    /// </summary>
    /// <param name="ent">Actions owner</param>
    /// <param name="item">The object whose sprite the action will take</param>
    /// <param name="toggle">Should it be false="in" or true="out" icon in the lower right corner of the action</param>
    private void UpdateToggleAction(Entity<InnerHandToggleableComponent> ent, EntityUid item, bool toggle)
    {
        if (!_actionsSystem.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action))
            return;

        if (!TryComp<ActionComponent>(ent.Comp.ActionEntity, out var action))
            return;

        _actionsSystem.SetEntityIcon((ent.Comp.ActionEntity.Value, action), item);
        _actionsSystem.SetToggled(ent.Comp.ActionEntity, toggle);

        if (toggle)
            _metaData.SetEntityName(ent.Comp.ActionEntity.Value, Loc.GetString("action-inner-hand-toggle-name-out"));
        else
            _metaData.SetEntityName(ent.Comp.ActionEntity.Value, Loc.GetString("action-inner-hand-toggle-name-in"));

    }

    private void OnToggleInnerHand(Entity<InnerHandToggleableComponent> ent, ref ToggleInnerHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<HandsComponent>(ent, out var handsComp))
            return;

        if (handsComp.ActiveHand == null)
            return;

        if (!ent.Comp.HandsContainers.TryGetValue(handsComp.ActiveHand.Name, out var innerToggle))
            return;

        if (innerToggle.Container == null)
            return;

        if (ent.Comp.ActionEntity == null)
            return;

        if (innerToggle.InnerItemUid != null && !handsComp.ActiveHand.IsEmpty)
        {
            _popup.PopupClient(Loc.GetString("action-inner-hand-toggle-activehand-full-popup"), ent, ent);
        }

        if (innerToggle.InnerItemUid != null && handsComp.ActiveHand.IsEmpty)
        {
            if (_hand.TryPickup(ent, innerToggle.InnerItemUid.Value, handsComp.ActiveHand))
            {
                innerToggle.InnerItemUid = null;
                _actionsSystem.SetToggled(ent.Comp.ActionEntity, false);// we don't update the whole action because the hand and the action do not change
                _metaData.SetEntityName(ent.Comp.ActionEntity.Value, Loc.GetString("action-inner-hand-toggle-name-in"));
                return;
            }
        }

        if (innerToggle.InnerItemUid == null && handsComp.ActiveHand.HeldEntity != null)
        {
            if (TryComp<StuckOnEquipComponent>(handsComp.ActiveHand.HeldEntity, out var stuckOnEquip))
                _stuckOnEquip.UnstuckItem((handsComp.ActiveHand.HeldEntity.Value, stuckOnEquip));

            innerToggle.InnerItemUid = handsComp.ActiveHand.HeldEntity.Value;
            _containerSystem.Insert((handsComp.ActiveHand.HeldEntity.Value, null, null), innerToggle.Container);
            _actionsSystem.SetToggled(ent.Comp.ActionEntity, true); // we don't update the whole action because the hand and the action do not change
            _metaData.SetEntityName(ent.Comp.ActionEntity.Value, Loc.GetString("action-inner-hand-toggle-name-out"));
            return;
        }
    }
}
