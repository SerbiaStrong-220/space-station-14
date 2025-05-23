// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands;
using Content.Shared.Interaction.Events;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// </summary>
public sealed class InnerHandToggleProviderSystemSystem : EntitySystem
{
    [Dependency] private readonly SharedInnerHandToggleableSystem _innerHand = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerHandToggleProviderComponent, GotEquippedHandEvent>(OnEquip);
        SubscribeLocalEvent<InnerHandToggleProviderComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<InnerHandToggleProviderComponent, DroppedEvent>(OnDrop);
    }

    private void OnEquip(Entity<InnerHandToggleProviderComponent> ent, ref GotEquippedHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var inner = EnsureComp<InnerHandToggleableComponent>(args.User);

        _innerHand.TryCreateInnerHandSpace((args.User, inner), ent, args.Hand);
    }

    private void OnUnequip(Entity<InnerHandToggleProviderComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

    }

    private void OnDrop(Entity<InnerHandToggleProviderComponent> ent, ref DroppedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
    }
}
