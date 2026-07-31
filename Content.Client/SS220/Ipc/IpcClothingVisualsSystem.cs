// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Item;
using Content.Shared.SS220.Ipc;

namespace Content.Client.SS220.Ipc;

/// <summary>
/// Prevents certain equipment visual layers (eyewear, earwear) from ever being added
/// to an IPC's sprite, since IPC sprites don't have matching layers for it.
/// TODO - replace by ipc module system
/// </summary>
public sealed partial class IpcClothingVisualsSystem : EntitySystem
{
    private static readonly HashSet<string> HiddenSlots = ["eyes", "ears"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemComponent, GetEquipmentVisualsEvent>(
            OnGetVisuals,
            after: [typeof(ClientClothingSystem)]);
    }

    private void OnGetVisuals(Entity<ItemComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!HiddenSlots.Contains(args.Slot))
            return;

        if (!HasComp<IpcComponent>(args.Equipee))
            return;

        args.Layers.Clear();
    }
}