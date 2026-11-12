// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Clothing.Components;

/// <summary>
/// While worn on the neck, items in <see cref="Slots"/> are not drawn on the wearer.
/// Lets cloaks cover tanks and bags that would otherwise stick out from under them.
/// </summary>
[RegisterComponent]
public sealed partial class HideEquipmentLayersClothingComponent : Component
{
    [DataField]
    public HashSet<string> Slots = ["back", "suitstorage"];

    [DataField]
    public EntityWhitelist Whitelist = new() { Components = ["GasTank", "Storage"] };

    /// <summary>
    /// Weapons stay visible, even ones with storage like the pneumatic cannon.
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new() { Components = ["Gun"] };
}
