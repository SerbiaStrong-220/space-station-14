// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.RestrictedItem;

[RegisterComponent]
public sealed partial class RestrictedStorageComponent : Component
{
    /// <summary>
    /// A whitelist for selecting which entity can interact with storage
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Which slots should the storage work in.
    /// If null, the storage will work in any slot and even outside it (for example, on the floor)
    /// </summary>
    [DataField]
    public SlotFlags? Slots;
}
