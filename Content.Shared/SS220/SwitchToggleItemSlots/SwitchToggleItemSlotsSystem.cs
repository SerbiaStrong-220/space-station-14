using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SwitchToggleItemSlots;

public sealed class SwitchToggleItemSlotsSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SwitchToggleItemSlotsComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<SwitchToggleItemSlotsComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<SwitchToggleItemSlotsComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SwitchToggleItemSlotsComponent, SwitchToggleItemSlotEvent>(OnDoAfter);

        SubscribeLocalEvent<SwitchToggleItemSlotsComponent, ExaminedEvent>(OnExamine);
    }

    private void OnCompInit(Entity<SwitchToggleItemSlotsComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, SwitchToggleItemSlotsComponent.HiddenSlot, ent.Comp.HiddenItemSlot);
    }

    private void OnCompRemove(Entity<SwitchToggleItemSlotsComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.HiddenItemSlot);
    }

    private void OnInteractUsing(Entity<SwitchToggleItemSlotsComponent> ent, ref InteractUsingEvent args)
    {
        var item = args.Used;
        var user = args.User;

        if (!_tool.HasQuality(item, ent.Comp.NeedTool))
            return;

        if (!HasComp<ItemSlotsComponent>(ent.Owner))
            return;

        if (!_itemSlots.TryGetSlot(ent.Owner, SwitchToggleItemSlotsComponent.HiddenSlot, out _))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            ent.Comp.TimeToOpen,
            new SwitchToggleItemSlotEvent(),
            ent.Owner,
            item)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            DuplicateCondition = DuplicateConditions.None,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;

    }

    private void OnDoAfter(Entity<SwitchToggleItemSlotsComponent> ent, ref SwitchToggleItemSlotEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (ent.Comp.HiddenItemSlot.Locked)
        {
            _itemSlots.SetLock(ent.Owner, ent.Comp.HiddenItemSlot, false);
            _audio.PlayPredicted(ent.Comp.SoundOpen, ent.Owner, args.User);
            return;
        }

        _itemSlots.SetLock(ent.Owner, ent.Comp.HiddenItemSlot, true);
        _audio.PlayPredicted(ent.Comp.SoundClosed, ent.Owner, args.User);
    }

    private void OnExamine(Entity<SwitchToggleItemSlotsComponent> ent, ref ExaminedEvent args)
    {
        if(!ent.Comp.HiddenItemSlot.Locked)
            args.PushMarkup(Loc.GetString("switch-toggle-item-slots-examine-open"));
    }
}

[Serializable]
[NetSerializable]
public sealed partial class SwitchToggleItemSlotEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
