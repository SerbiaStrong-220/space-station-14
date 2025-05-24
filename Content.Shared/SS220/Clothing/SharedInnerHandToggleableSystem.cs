// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.StuckOnEquip;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// </summary>
public abstract class SharedInnerHandToggleableSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hand = default!;
    [Dependency] private readonly SharedStuckOnEquipSystem _stuckOnEquip = default!;

    public const string InnerHandPrefix = "inner_";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerHandToggleableComponent, ToggleInnerHandEvent>(OnToggleInnerHand);
        SubscribeLocalEvent<InnerHandToggleableComponent, ProvideToggleInnerHandEvent>(OnProvideInnerHand);
        SubscribeLocalEvent<InnerHandToggleableComponent, RemoveToggleInnerHandEvent>(OnRemoveInnerHand);
    }
    private void OnProvideInnerHand(Entity<InnerHandToggleableComponent> ent, ref ProvideToggleInnerHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        int unusedPrefix = SharedBodySystem.PartSlotContainerIdPrefix.Length;
        var name = string.Concat(InnerHandPrefix, args.Hand.AsSpan(unusedPrefix)); ;//add and delete shit

        if (ent.Comp.HandsContainers.ContainsKey(name))
            return;

        if (!TryGetInnerHandProto(name, out var proto))
            return;

        if (proto is null)//Made this shit cause rn idk how to be without it
            return;

        var manager = EnsureComp<ContainerManagerComponent>(ent);

        var handInfo = new ToggleableHandInfo
        {
            Action = proto.Action,
            Container = _containerSystem.EnsureContainer<ContainerSlot>(ent, name, manager),
            ContainerId = name,
            InnerItemUid = args.Hidable
        };

        //some copypaste from ToggleableClothingSystem
        if (!_actionContainer.EnsureAction(ent, ref handInfo.ActionEntity, out var action, handInfo.Action))
            return;

        _actionsSystem.SetEntityIcon(handInfo.ActionEntity.Value, handInfo.InnerItemUid, action);
        _actionsSystem.AddAction(ent, ref handInfo.ActionEntity, handInfo.Action);//cant add cation without it

        ent.Comp.HandsContainers.Add(name, handInfo);

        args.Hidable.Comp.ContainerName = name;
        args.Hidable.Comp.InnerUser = ent;
        args.Hidable.Comp.HandName = args.Hand;
    }

    private void OnRemoveInnerHand(Entity<InnerHandToggleableComponent> ent, ref RemoveToggleInnerHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var handInfo = ent.Comp.HandsContainers[args.HandContainer];
        _actionsSystem.RemoveAction(handInfo.ActionEntity);
        ent.Comp.HandsContainers.Remove(args.HandContainer);
        //I didn't find how to delete containers and I'm not sure if it's necessary
    }

    private void OnToggleInnerHand(Entity<InnerHandToggleableComponent> ent, ref ToggleInnerHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!ent.Comp.HandsContainers.TryGetValue(args.Hand, out var handInfo))
            return;

        if (handInfo.Container is null)
            return;

        if (handInfo.InnerItemUid is null)
            return;

        if (handInfo.Container.ContainedEntity != null)
        {
            _hand.TryPickup(ent, handInfo.InnerItemUid.Value, SharedBodySystem.PartSlotContainerIdPrefix + args.Hand.Substring(InnerHandPrefix.Length));
            _actionsSystem.SetToggled(handInfo.ActionEntity, false);
            return;
        }

        if (TryComp<StuckOnEquipComponent>(ent, out var stuckOnEquip))
            _stuckOnEquip.UnstuckItem((ent, stuckOnEquip));

        _containerSystem.Insert(handInfo.InnerItemUid.Value, handInfo.Container, force: false);
        _actionsSystem.SetToggled(handInfo.ActionEntity, true);

    }

    //i tried to get action based on hand, but idk how to do it rn
    private bool TryGetInnerHandProto(string handName, out InnerHandActionPrototype? proto)
    {
        proto = null;

        foreach (var handProto in _proto.EnumeratePrototypes<InnerHandActionPrototype>())
        {
            if (handProto.ID != handName)
                continue;

            proto = handProto;
            return true;
        }
        return false;
    }
}
