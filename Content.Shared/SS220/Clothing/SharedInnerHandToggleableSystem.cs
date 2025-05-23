// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt


using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Clothing;

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
    /// Postfix for any inner hand.
    /// </summary>
    public const string InnerHandPostfix = "_inner";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerHandToggleableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<InnerHandToggleableComponent, ToggleInnerHandEvent>(OnToggleInnerHand);
    }

    private void OnInit(Entity<InnerHandToggleableComponent> ent, ref ComponentInit args)
    {
        //ent.Comp.Container = _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    public void TryCreateInnerHandSpace(Entity<InnerHandToggleableComponent> ent, EntityUid hidable, Hand hand)
    {
        int unusedPrefix = SharedBodySystem.PartSlotContainerIdPrefix.Length;
        var name = hand.Name.Substring(unusedPrefix, hand.Name.Length - unusedPrefix); ;//add shit
        name = name + InnerHandPostfix; //delete shit

        if (ent.Comp.HandsContainers.ContainsKey(name))
            return;

        if (!TryGetInnerHandProto(name, out var proto))
            return;

        if (proto is null)//Made this shit cause rn idk how to fix it
            return;

        var manager = EnsureComp<ContainerManagerComponent>(ent);

        var handInfo = new ToggleableHandInfo
        {
            Action = proto.Action,
            Container = _containerSystem.EnsureContainer<ContainerSlot>(ent, name, manager),
            ContainerId = name,
            InnerItemUid = hidable
        };

        //some copypaste from ToggleableClothingSystem
        if (!_actionContainer.EnsureAction(ent, ref handInfo.ActionEntity, out var action, handInfo.Action))
            return;

        _actionsSystem.SetEntityIcon(handInfo.ActionEntity.Value, handInfo.InnerItemUid, action);
        _actionsSystem.AddAction(ent, ref handInfo.ActionEntity, handInfo.Action);//cant add cation without it

        ent.Comp.HandsContainers.Add(name, handInfo);
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

        if (item.Container.ContainedEntity == null)
        {
            //_hand.TryDrop(ent, item.InnerItemUid.Value);
            _containerSystem.Insert(item.InnerItemUid.Value, item.Container);
        }
        else
        {
            _hand.TryPickup(ent, item.InnerItemUid.Value);
        }
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


public sealed partial class ToggleInnerHandEvent : InstantActionEvent
{
    [DataField(required: true)]
    public string Hand = "middle";
}
