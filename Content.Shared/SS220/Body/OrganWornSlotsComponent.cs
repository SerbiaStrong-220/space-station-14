// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.DisplacementMap;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Body;

/// <summary>
/// Inventory slots worn on organs, e.g. gloves on hands. A slot whose organs are all missing is emptied and can't be equipped,
/// with some of them missing its clothes are drawn through the mask of the missing organ.
/// </summary>
[RegisterComponent]
public sealed partial class OrganWornSlotsComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<OrganCategoryPrototype>, SlotFlags> Slots = new();

    /// <summary>
    /// Displacement maps hiding the part of the clothes worn on the missing organ.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, DisplacementData> Masks = new();

    /// <summary>
    /// Used instead of <see cref="Masks"/> on clothing layers the species displacement doesn't apply to (species-specific sprites),
    /// needed only by species that displace these slots, as their masks displace too.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, DisplacementData> UndisplacedMasks = new();
}
