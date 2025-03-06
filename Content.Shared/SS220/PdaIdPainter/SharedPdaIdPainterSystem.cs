using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.PdaIdPainter;

public abstract class SharedPdaIdPainterSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public readonly HashSet<EntityPrototype> PdaAndIdProtos = new();

    public override void Initialize()
    {
        base.Initialize();

        GetAllVariants();

        SubscribeLocalEvent<PdaIdPainterComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<PdaIdPainterComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<PdaIdPainterComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<PdaIdPainterComponent, EntRemovedFromContainerMessage>(OnRemove);

        SubscribeLocalEvent<PdaIdPainterComponent, PdaIdPainterPickedPdaMessage>(OnPdaPicked);
        SubscribeLocalEvent<PdaIdPainterComponent, PdaIdPainterPickedIdMessage>(OnIdPicked);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReloaded);
    }

    private void OnCompInit(Entity<PdaIdPainterComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, PdaIdPainterComponent.IdPainterSlot, ent.Comp.IdCardSlot);
        _itemSlots.AddItemSlot(ent.Owner, PdaIdPainterComponent.PdaPainterSlot, ent.Comp.PdaSlot);
    }

    private void OnCompRemove(Entity<PdaIdPainterComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.IdCardSlot);
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.PdaSlot);
    }

    private void OnInsert(Entity<PdaIdPainterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_net.IsClient)
            return;

        var targetPda = GetNetEntity(ent.Comp.PdaSlot.Item);
        var targetId = GetNetEntity(ent.Comp.IdCardSlot.Item);

        _ui.SetUiState(ent.Owner, PdaIdPainterUiKey.Key, new PdaIdPainterBoundState(targetId, targetPda));
    }

    private void OnRemove(Entity<PdaIdPainterComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_net.IsClient)
            return;

        var targetPda = GetNetEntity(ent.Comp.PdaSlot.Item);
        var targetId = GetNetEntity(ent.Comp.IdCardSlot.Item);

        _ui.SetUiState(ent.Owner, PdaIdPainterUiKey.Key, new PdaIdPainterBoundState(targetId, targetPda));
    }

    private void OnPdaPicked(Entity<PdaIdPainterComponent> ent, ref PdaIdPainterPickedPdaMessage args)
    {
        if (!IsAllowed(ent.Owner, args.Actor))
            return;

        if (!_proto.TryIndex(args.Proto, out var prototype))
            return;

        var target = ent.Comp.PdaSlot.Item;

        if (target != null && TryComp<PdaIdPainterTargetComponent>(target, out var targetComp))
            Dirty(target.Value, targetComp);

        if (!PdaAndIdProtos.Contains(prototype))
            return;

        ent.Comp.PdaChosenProto = args.Proto;
        Dirty(ent);

        UpdateVisuals(ent.Comp.PdaChosenProto, target);
    }

    private void OnIdPicked(Entity<PdaIdPainterComponent> ent, ref PdaIdPainterPickedIdMessage args)
    {
        if (!IsAllowed(ent.Owner, args.Actor))
            return;

        if (!_proto.TryIndex(args.Proto, out var prototype))
            return;

        var target = ent.Comp.IdCardSlot.Item;

        if (target != null && TryComp<PdaIdPainterTargetComponent>(target, out var targetComp))
        {
            Dirty(target.Value, targetComp);
        }

        if (!PdaAndIdProtos.Contains(prototype))
            return;

        ent.Comp.IdChosenProto = prototype;
        Dirty(ent);

        UpdateVisuals(ent.Comp.IdChosenProto, target);
    }

    private void OnProtoReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            GetAllVariants();
    }

    private void GetAllVariants()
    {
        PdaAndIdProtos.Clear();

        var prototypes = _proto.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            if (!IsValidTarget(proto))
                continue;

            PdaAndIdProtos.Add(proto);
        }
    }

    private bool IsAllowed(EntityUid entity, EntityUid owner)
    {
        if (!TryComp<AccessReaderComponent>(entity, out var access))
            return true;

        if (_access.IsAllowed(owner, entity, access))
            return true;

        _popup.PopupEntity(Loc.GetString("pda-id-painter-not-enough-permissions"), owner, owner);
        return false;
    }

    private bool IsValidTarget(EntityPrototype proto)
    {
        if (proto.Abstract || proto.HideSpawnMenu)
            return false;

        return proto.TryGetComponent(out TagComponent? tag, _factory) && _tag.HasTag(tag, "WhitelistPdaIdPainter");
    }

    protected void UpdateVisuals(EntProtoId? ent, EntityUid? target)
    {
        if (target == null)
            return;

        var isIdCard = HasComp<IdCardComponent>(target);

        if (string.IsNullOrEmpty(ent) || !_proto.TryIndex(ent, out var proto))
            return;

        UpdateSprite(target.Value, proto);

        if (TryComp(target, out ClothingComponent? clothing) &&
            proto.TryGetComponent(out ClothingComponent? otherClothing, _factory))
        {
            _clothing.CopyVisuals(target.Value, otherClothing, clothing);
        }

        if (TryComp(target, out ItemComponent? item) &&
            proto.TryGetComponent(out ItemComponent? otherItem, _factory))
        {
            _item.CopyVisuals(target.Value, otherItem, item);
        }

        if (TryComp(target, out AppearanceComponent? appearance) &&
            proto.TryGetComponent("Appearance", out AppearanceComponent? appearanceOther))
        {
            _appearance.AppendData(appearanceOther, target.Value);
            Dirty(target.Value, appearance);
        }

        if (!isIdCard)
        {
            var meta = MetaData(target.Value);
            _metaData.SetEntityName(target.Value, proto.Name, meta);
            _metaData.SetEntityDescription(target.Value, proto.Description, meta);
        }

        var targetComp = EnsureComp<PdaIdPainterTargetComponent>(target.Value);
        targetComp.NewProto = proto;

        Dirty(target.Value, targetComp);
    }

    protected virtual void UpdateSprite(EntityUid uid, EntityPrototype proto) { }
}
