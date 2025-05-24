// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.Clothing;

namespace Content.Server.SS220.Clothing;

/// <summary>
/// Handles adding and using a toggle action for <see cref="ToggleClothingComponent"/>.
/// </summary>
public sealed class SharedInnerHandToggleableSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hand = default!;


    /// <summary>
    /// Prefix for any inner hand.
    /// </summary>
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

        if (!ent.Comp.HandsContainers.TryGetValue(args.Hand, out var item))
            return;

        if (item.Container is null)
            return;

        if (item.InnerItemUid is null)
            return;

        if (item.Container.ContainedEntity != null)
        {
            _hand.TryPickup(ent, item.InnerItemUid.Value, SharedBodySystem.PartSlotContainerIdPrefix + args.Hand.Substring(InnerHandPrefix.Length));
            return;
        }

        /*
        if (TryComp<UnremoveableComponent>(item.InnerItemUid.Value, out var uremovable) && uremovable.LockToHands)//wierd construction idk how to rewrite it
        {
            uremovable.LockToHands = false;
            _containerSystem.Insert(item.InnerItemUid.Value, item.Container);
            uremovable.LockToHands = true;
        }
        else
        */
        _containerSystem.Insert(item.InnerItemUid.Value, item.Container, force: false);

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
