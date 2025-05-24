// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Systems;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// </summary>
public sealed class InnerHandToggleProviderSystemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerHandToggleProviderComponent, GotEquippedHandEvent>(OnHandEquip);
        SubscribeLocalEvent<InnerHandToggleProviderComponent, EntGotInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<InnerHandToggleProviderComponent, DroppedEvent>(OnDrop);
    }

    private void OnHandEquip(Entity<InnerHandToggleProviderComponent> ent, ref GotEquippedHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.InnerUser != null)
            return;

        if (ent.Comp.ContainerName != null)
            return;

        EnsureComp<InnerHandToggleableComponent>(args.User);

        var ev = new ProvideToggleInnerHandEvent(ent, args.Hand.Name);
        RaiseLocalEvent(args.User, ev);
    }
    private void OnEntInserted(Entity<InnerHandToggleProviderComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var newContainerName = args.Container.ID;

        if (!TryRemoveToggle(ent, newContainerName))
            return;

        if (newContainerName.Contains(SharedBodySystem.PartSlotContainerIdPrefix)) //handle hand to hand movement
        {
            var provEv = new ProvideToggleInnerHandEvent(ent, newContainerName);
            RaiseLocalEvent(args.Container.Owner, provEv);
        }
    }

    private void OnDrop(Entity<InnerHandToggleProviderComponent> ent, ref DroppedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryRemoveToggle(ent, ""))
            return;
    }

    private bool TryRemoveToggle(Entity<InnerHandToggleProviderComponent> ent, string newContainerName)
    {
        if (ent.Comp.InnerUser is null)
            return false;

        if (ent.Comp.ContainerName is null)
            return false;

        if (ent.Comp.HandName == newContainerName)
            return false;

        if (ent.Comp.ContainerName == newContainerName)
            return false;

        var remEv = new RemoveToggleInnerHandEvent(ent, ent.Comp.ContainerName);
        RaiseLocalEvent(ent.Comp.InnerUser.Value, remEv);

        ent.Comp.ContainerName = null;
        ent.Comp.HandName = null;
        ent.Comp.InnerUser = null;

        return true;
    }
}
