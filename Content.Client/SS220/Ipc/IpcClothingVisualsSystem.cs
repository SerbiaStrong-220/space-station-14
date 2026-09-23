// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Clothing;
using Content.Shared.SS220.Ipc;

namespace Content.Client.SS220.Ipc;

/// <summary>
/// Prevents certain equipment visual layers (eyewear, earwear) from ever being added
/// to an IPC's sprite, since IPC sprites don't have matching layers for it.
/// TODO - replace by ipc module system
/// </summary>
public sealed partial class IpcClothingVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IpcComponent, BeforeRenderEquipmentEvent>(OnBeforeRenderEquipment);
    }

    private void OnBeforeRenderEquipment(Entity<IpcComponent> ent, ref BeforeRenderEquipmentEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.HiddenClothingSlots.Contains(args.Slot))
            args.Cancel();
    }
}