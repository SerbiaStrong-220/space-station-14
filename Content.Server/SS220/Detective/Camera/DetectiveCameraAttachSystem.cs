// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Construction.Components;
using Content.Server.Popups;
using Content.Server.SurveillanceCamera;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.SS220.Detective.Camera;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.Detective.Camera;

public sealed class DetectiveCameraAttachSystem : SharedDetectiveCameraAttachSystem
{
    private readonly static ProtoId<TagPrototype> DetectiveCameraKey = "DetectiveCamera";

    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetectiveCameraAttachComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DetectiveCameraAttachComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DetectiveCameraAttachComponent, DetectiveCameraAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<DetectiveCameraAttachComponent, DetectiveCameraDetachDoAfterEvent>(OnDetachDoAfter);
    }

    private void OnComponentStartup(Entity<DetectiveCameraAttachComponent> entity, ref ComponentStartup args)
    {
        if (!TryComp<SurveillanceCameraComponent>(entity, out var camera))
            return;

        _camera.SetActive(entity, false, camera);
    }

    private void OnAfterInteract(Entity<DetectiveCameraAttachComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (entity.Comp.Attached || !IsAttachable(target, entity.Comp))
            return;

        if (!TryAttachCamera(target, entity, args.User))
            return;

        args.Handled = true;
    }

    private void OnAttachDoAfter(EntityUid uid, DetectiveCameraAttachComponent component, DetectiveCameraAttachDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (HasComp<AttachedCameraComponent>(args.AttachTarget))
            return;

        if (!TryComp<SurveillanceCameraComponent>(uid, out var cameraComponent))
            return;

        _camera.SetActive(uid, true, cameraComponent);

        AddCameraItemSlotsComponent(args.AttachTarget, args.User, component.CellSlotId);

        var attachedCameraComp = EnsureComp<AttachedCameraComponent>(args.AttachTarget);
        attachedCameraComp.AttachedCamera = uid;
        attachedCameraComp.UserOwner = args.User;
        attachedCameraComp.CellSlotId = component.CellSlotId;

        component.Attached = true;
        _popup.PopupEntity(Loc.GetString("detective-camera-attached"), uid, args.User);

        Dirty(uid, component);
        args.Handled = true;
    }

    private void OnDetachDoAfter(EntityUid uid, DetectiveCameraAttachComponent component, DetectiveCameraDetachDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!HasComp<AttachedCameraComponent>(args.DetachTarget))
            return;

        if (!TryComp<SurveillanceCameraComponent>(uid, out var cameraComponent))
            return;

        _camera.SetActive(uid, false, cameraComponent);

        RemoveCameraItemSlotsComponent(args.DetachTarget, args.User);

        if (!RemComp<AttachedCameraComponent>(args.DetachTarget))
            return;

        component.Attached = false;
        _popup.PopupEntity(Loc.GetString("detective-camera-detached"), uid, args.User);

        Dirty(uid, component);
        args.Handled = true;
    }

    public bool TryDetachVerb(EntityUid uid, EntityUid target, EntityUid user)
    {
        if (!TryComp<DetectiveCameraAttachComponent>(uid, out var component))
            return false;

        if (!component.Attached)
            return false;

        if (!TryDetachCamera(target, (uid, component), user))
            return false;

        return true;
    }

    private void AddCameraItemSlotsComponent(EntityUid uid, EntityUid user, string cellSlotId)
    {
        EnsureComp<ItemSlotsComponent>(uid);

        var detectiveCameraSlot = new ItemSlot();

        detectiveCameraSlot.Whitelist = new EntityWhitelist()
        {
            Tags = new List<ProtoId<TagPrototype>>()
            {
                DetectiveCameraKey
            }
        };

        _itemSlots.AddItemSlot(uid, cellSlotId, detectiveCameraSlot);
        _itemSlots.TryInsertFromHand(uid, detectiveCameraSlot, user);

        detectiveCameraSlot.Locked = true;
    }

    private void RemoveCameraItemSlotsComponent(EntityUid uid, EntityUid user)
    {
        if (!TryComp<ItemSlotsComponent>(uid, out var component))
            return;

        var detectiveCameraSlot = component.Slots.FirstOrDefault().Value;

        detectiveCameraSlot.Locked = false;

        _itemSlots.TryEjectToHands(uid, detectiveCameraSlot, user);

        RemComp<ItemSlotsComponent>(uid);
    }

    private bool IsAttachable(EntityUid target, DetectiveCameraAttachComponent component)
    {
        if (!HasComp<ConstructionComponent>(target) && !HasComp<ClothingComponent>(target))
            return false;

        if (HasComp<AttachedCameraComponent>(target))
            return false;

        if (component.Attached)
            return false;

        return true;
    }
}
