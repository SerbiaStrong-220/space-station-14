
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Attachables.Systems;

public sealed partial class AttachablesContainerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainerSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainerSystem = default!;

    [Dependency] private readonly SharedUserInterfaceSystem _sharedUserInterfaceSystem = default!;
    [Dependency] private readonly SharedActionsSystem _sharedActionsSystem = default!;
    

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttachablesContainerComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<AttachablesContainerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AttachablesContainerComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<AttachablesContainerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<AttachablesContainerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

    }

    #region Main component events

    public void OnMapInit(EntityUid uid, AttachablesContainerComponent component, MapInitEvent args)
    {
        _actionContainerSystem.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

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

    #region Messages

    public void OnEntInserted(EntityUid uid, AttachablesContainerComponent component, EntInsertedIntoContainerMessage args) { }
    public void OnEntRemoved(EntityUid uid, AttachablesContainerComponent component, EntRemovedFromContainerMessage args) { }

    #endregion

    #region I dunno
    protected void OnAttach() { }

    protected void OnDeattach() { }

    #endregion
}
