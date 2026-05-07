// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT

using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed class InternalMagazineSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalMagazineComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<InternalMagazineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<InternalMagazineComponent, ChangeInternalMagasineLockStatusDoAfterEvent>(OnMagStatusChanged);
    }

    private void OnInteractUsing(Entity<InternalMagazineComponent> ent, ref InteractUsingEvent args)
    {
        if (!ent.Comp.MagDetachable)
            return;

        var item = args.Used;
        var user = args.User;

        if (!_tool.HasQuality(item, ent.Comp.RequiredQuality))
            return;

        if (!HasComp<ItemSlotsComponent>(ent.Owner))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, ent.Comp.TimeToFix, new ChangeInternalMagasineLockStatusDoAfterEvent(), ent.Owner, item)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnMagStatusChanged(Entity<InternalMagazineComponent> ent, ref ChangeInternalMagasineLockStatusDoAfterEvent args)
    {
        ent.Comp.MagFixed = !ent.Comp.MagFixed;

        _itemSlots.SetLock(ent.Owner, ent.Comp.MagSlotId, ent.Comp.MagFixed);
    }

    private void OnComponentStartup(Entity<InternalMagazineComponent> ent, ref ComponentStartup args)
    {
        _itemSlots.SetLock(ent.Owner, ent.Comp.MagSlotId, ent.Comp.MagFixed);
    }

}

[Serializable, NetSerializable]
public sealed partial class ChangeInternalMagasineLockStatusDoAfterEvent : DoAfterEvent
{
    public ChangeInternalMagasineLockStatusDoAfterEvent()
    {
    }

    public override DoAfterEvent Clone() => this;
}
