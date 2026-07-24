// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.SS220.Ipc;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Ipc;

/// <summary>
/// Hides visual layers added by certain equipped clothing on IPCs,
/// since IPC sprites don't have matching layers for it.
/// TODO - replace by ipc module system
/// </summary>
public sealed partial class IpcClothingVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly HashSet<string> HiddenSlots = ["eyes", "ears"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnVisualsUpdated);
    }

    private void OnVisualsUpdated(EquipmentVisualsUpdatedEvent args)
    {
        if (!HiddenSlots.Contains(args.Slot))
            return;

        if (!HasComp<IpcComponent>(args.Equipee))
            return;

        if (!TryComp<SpriteComponent>(args.Equipee, out var sprite))
            return;

        foreach (var key in args.RevealedLayers.ToList())
        {
            _sprite.RemoveLayer((args.Equipee, sprite), key);
            args.RevealedLayers.Remove(key);
        }
    }
}