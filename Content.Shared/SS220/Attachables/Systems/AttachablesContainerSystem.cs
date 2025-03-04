
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Attachables.Systems;

public sealed partial class AttachablesContainerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttachablesContainerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AttachablesContainerComponent, ComponentRemove>(OnComponentRemove);

    }

    #region Main component events

    public void OnComponentInit(EntityUid uid, AttachablesContainerComponent component, ComponentInit args)
    {
        foreach (var (id, slot) in component.AllowedSlots)
        {
            _itemSlotsSystem.AddItemSlot(uid, id, slot);
            // _itemSlotsSystem.SetLock(uid, slot, true);
            _sharedContainerSystem.GetContainer(uid, id).OccludesLight = false;
        }
    }

    public void OnComponentRemove(EntityUid uid, AttachablesContainerComponent component, ComponentRemove args)
    {
        foreach (var (id, slot) in component.AllowedSlots)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, slot);
        }
    }

    #endregion

    #region I dunno
    protected void OnAttach() { }

    protected void OnDeattach() { }

    #endregion
}
