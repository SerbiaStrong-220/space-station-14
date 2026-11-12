// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Clothing.Components;

/// <summary>
/// While this clothing is equipped, the visual layers of matching items in the listed equipment slots are not drawn.
/// Used by cloaks so gas tanks and bags don't stick out from under them. Weapons are intentionally not hidden.
/// </summary>
[RegisterComponent]
public sealed partial class HideEquipmentLayersClothingComponent : Component
{
    /// <summary>
    /// Slot name -> which items in that slot get hidden.
    /// </summary>
    [DataField]
    public Dictionary<string, EntityWhitelist> Slots = new()
    {
        ["suitstorage"] = new() { Components = ["GasTank"] },
        ["back"] = new() { Components = ["Storage", "GasTank"] },
    };

    /// <summary>
    /// Items matching this are never hidden, even if they pass a slot whitelist (e.g. pneumatic cannon has storage).
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new() { Components = ["Gun"] };
}
