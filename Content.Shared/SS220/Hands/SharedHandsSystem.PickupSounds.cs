// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Containers;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    // Equip events fire synchronously during insertion. Keep suppression scoped
    // to this transfer so it cannot silence a later pickup from the floor.
    private readonly HashSet<EntityUid> _pickupsWithoutSound = new();

    private bool InsertIntoHandWithPickupSound(EntityUid item, BaseContainer handContainer, bool suppressPickupSound)
    {
        if (!suppressPickupSound)
            return ContainerSystem.Insert(item, handContainer);

        var added = _pickupsWithoutSound.Add(item);
        try
        {
            return ContainerSystem.Insert(item, handContainer);
        }
        finally
        {
            if (added)
                _pickupsWithoutSound.Remove(item);
        }
    }
}
