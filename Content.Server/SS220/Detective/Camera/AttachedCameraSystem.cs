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

    private void OnExamined(Entity<AttachedCameraComponent> ent, ref ExaminedEvent args)
    {
        TryGetCameraFromSlot(ent, out var detectiveCamera, ent);

        if (!args.IsInDetailsRange && detectiveCamera == null)
            return;

        args.PushMarkup(Loc.GetString("detective-camera-attached-description"), -1);
    }

    private void AddDetachVerbs(Entity<AttachedCameraComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        TryGetCameraFromSlot(ent, out var detectiveCamera, ent);

        if (ent.Comp.UserOwner == null || args.User != ent.Comp.UserOwner)
            return;

        if ((!args.CanInteract || !args.CanAccess) && detectiveCamera == null)
            return;

        var user = args.User;

        InteractionVerb detachVerb = new()
        {
            Text = Loc.GetString("detective-camera-detach-verb"),
            Act = () => _detectiveCameraAttach.TryDetachVerb(ent.Comp.AttachedCamera, ent, user),
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
