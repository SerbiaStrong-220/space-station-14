// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// </summary>
public sealed class InnerHandToggleProviderSystemSystem : EntitySystem
{
    [Dependency] private readonly SharedInnerHandToggleableSystem _innerHand = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

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

        var inner = EnsureComp<InnerHandToggleableComponent>(args.User);

        var ev = new ProvideToggleInnerHandEvent(ent, args.Hand);
        RaiseLocalEvent(args.User, ev);
    }
    private void OnEntInserted(Entity<InnerHandToggleProviderComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.InnerUser is null)
            return;

        if (ent.Comp.ContainerName is null)
            return;

        if (ent.Comp.HandName == args.Container.ID)
            return;

        if (ent.Comp.ContainerName == args.Container.ID)
            return;

        var ev = new RemoveToggleInnerHandEvent(ent, ent.Comp.ContainerName);
        RaiseLocalEvent(ent.Comp.InnerUser.Value, ev);

        ent.Comp.ContainerName = null;
        ent.Comp.HandName = null;
        ent.Comp.InnerUser = null;
    }

    private void OnDrop(Entity<InnerHandToggleProviderComponent> ent, ref DroppedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
    }
}
