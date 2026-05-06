using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.ToggleableItemSlot;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed class InternalMagazineSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalMagazineComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<InternalMagazineComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnInteractUsing(Entity<InternalMagazineComponent> ent, ref InteractUsingEvent args)
    {
        var item = args.Used;
        var user = args.User;

        if (!_tool.HasQuality(item, ent.Comp.RequiredQuality))
            return;

        if (!HasComp<ItemSlotsComponent>(ent.Owner))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            ent.Comp.TimeToFix,
            new ToggleableItemSlotEvent(),
            ent.Owner,
            item)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnComponentStartup(Entity<InternalMagazineComponent> ent, ref ComponentStartup args)
    {
        _itemSlots.SetLock(ent.Owner, ent.Comp.MagSlotId, ent.Comp.magFixed);
    }

}
