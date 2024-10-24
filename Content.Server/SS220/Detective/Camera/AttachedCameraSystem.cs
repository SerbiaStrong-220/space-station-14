// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Server.SS220.Detective.Camera;

public sealed class AttachedCameraSystem : EntitySystem
{
    [Dependency] private readonly DetectiveCameraAttachSystem _detectiveCameraAttach = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttachedCameraComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AttachedCameraComponent, GetVerbsEvent<InteractionVerb>>(AddDetachVerbs);
    }

    private void OnExamined(EntityUid uid, AttachedCameraComponent component, ExaminedEvent args)
    {
        TryGetCameraFromSlot(uid, out var detectiveCamera, component);

        if (!args.IsInDetailsRange && detectiveCamera == null)
            return;

        args.PushMarkup(Loc.GetString("detective-camera-attached-description"), -1);
    }

    private void AddDetachVerbs(EntityUid uid, AttachedCameraComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        TryGetCameraFromSlot(uid, out var detectiveCamera, component);

        if (component.UserOwner == null || args.User != component.UserOwner)
            return;

        if ((!args.CanInteract || !args.CanAccess) && detectiveCamera == null)
            return;

        InteractionVerb detachVerb = new()
        {
            Text = $"{Loc.GetString("detective-camera-detach-verb")}",
            Act = () => _detectiveCameraAttach.TryDetachVerb(component.AttachedCamera, uid, args.User)
        };

        args.Verbs.Add(detachVerb);
    }

    public bool TryGetCameraFromSlot(EntityUid uid, out DetectiveCameraComponent? detectiveCamera, AttachedCameraComponent? component = null)
    {
        detectiveCamera = null;

        if (!Resolve(uid, ref component))
            return false;

        if (!_itemSlots.TryGetSlot(uid, component.CellSlotId, out var itemSlot))
            return false;

        var cameraEnt = itemSlot.Item;
        return TryComp(cameraEnt, out detectiveCamera);
    }
}
