using Content.Shared.Actions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Xml.Linq;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// Handles adding and using a toggle action for <see cref="ToggleClothingComponent"/>.
/// </summary>
public sealed class SharedInnerHandToggleableSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerHandToggleableComponent, ComponentInit>(OnInit);
        //SubscribeLocalEvent<InnerHandToggleableComponent, ToggleClothingEvent>(OnToggleItem);
    }

    private void OnInit(Entity<InnerHandToggleableComponent> ent, ref ComponentInit args)
    {
        //ent.Comp.Container = _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    public void TryCreateInnerHandSpace(Entity<InnerHandToggleableComponent> ent, EntityUid hidable, Hand hand)
    {
        var name = hand.Name + "_innerHand";

        if (ent.Comp.HandsContainers.ContainsKey(name))
            return;

        if (!TryGetInnerHandProto(name, out var proto))
            return;

        if (proto is null)//Made this shit cause rn idk how to fix it
            return;

        var handInfo = new ToggleableHandInfo();

        handInfo.Action = proto.Action;
        handInfo.ContainerId = name;
        handInfo.InnerItemUid = hidable;

        var manager = EnsureComp<ContainerManagerComponent>(ent);
        var container = _containerSystem.EnsureContainer<ContainerSlot>(ent, name, manager);
    }

    private bool TryGetInnerHandProto(string handName, out ToggleableInnerHandPrototype? proto)
    {
        proto = null;

        foreach (var handProto in _proto.EnumeratePrototypes<ToggleableInnerHandPrototype>())
        {
            if (handProto.ID != handName)
                continue;

            proto = handProto;
            return true;
        }
        return false;
    }
}
