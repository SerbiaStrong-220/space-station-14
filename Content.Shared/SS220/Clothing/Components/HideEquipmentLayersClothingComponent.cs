// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Clothing.Components;

/// <summary>
/// While this clothing is equipped, the visual layers of matching items in the listed equipment slots are not drawn.
/// Used by cloaks so gas tanks in suit storage don't stick out from under them.
/// </summary>
[RegisterComponent]
public sealed partial class HideEquipmentLayersClothingComponent : Component
{
    [DataField]
    public HashSet<string> Slots = new() { "suitstorage" };

    /// <summary>
    /// Only items matching this whitelist are hidden. Weapons are intentionally not hidden.
    /// </summary>
    [DataField]
    public EntityWhitelist Whitelist = new() { Components = ["GasTank"] };
}
