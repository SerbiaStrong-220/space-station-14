// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using System.Linq;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Storage;

namespace Content.Shared.SS220.StorageClothingBlocker;

public sealed partial class StorageClothingBlockerSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StorageClothingBlockerComponent, StorageInteractAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<StorageClothingBlockerComponent, DidEquipEvent>(OnGotEquipped);
    }

    private void OnInteractAttempt(Entity<StorageClothingBlockerComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var enumerator = _inventory.GetSlotEnumerator(ent.Owner, ent.Comp.SlotFlags);
        if (enumerator.NextItem(out _) && args.User != ent.Owner)
            args.Cancelled = true;
    }

    private void OnGotEquipped(Entity<StorageClothingBlockerComponent> ent, ref DidEquipEvent args)
    {
        if (args.EquipTarget != ent.Owner)
            return;

        if ((args.SlotFlags & ent.Comp.SlotFlags) != SlotFlags.NONE)
            RemoveAllViewers(ent.Owner);
    }

    private void RemoveAllViewers(EntityUid storage)
    {
        var actors = _ui.GetActors(storage, StorageComponent.StorageUiKey.Key).ToList();
        foreach (var actor in actors)
        {
            if (actor == storage)
                continue;

            if (HasComp<BypassInteractionChecksComponent>(actor))
                continue;

            _ui.CloseUi(storage, StorageComponent.StorageUiKey.Key, actor);
        }
    }
}
