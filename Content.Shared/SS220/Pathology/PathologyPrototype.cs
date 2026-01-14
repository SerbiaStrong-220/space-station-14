// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed class PathologyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    // It kinda useless for some of them...
    [DataField]
    public SlotFlags? ArmorSlotFlags = null;

    [DataField]
    public PathologyDefinition[] Definition = Array.Empty<PathologyDefinition>();
}

[DataDefinition]
public sealed partial class PathologyDefinition
{
    [DataField(required: true)]
    public LocId Description;

    [DataField]
    public LocId? ProgressPopup;

    [DataField]
    public int MaxStackCount = PathologySystem.OneStack;

    [DataField]
    public HashSet<EntProtoId> StatusEffects = new();

    [DataField]
    public ProtoId<TraitPrototype>? Trait;

    [DataField]
    public PathologyProgressCondition[] ProgressConditions = Array.Empty<PathologyProgressCondition>();

    /// <summary>
    /// Called every update interval
    /// </summary>
    [DataField]
    public IPathologyEffect[] Effects = Array.Empty<IPathologyEffect>();

    /// <summary>
    /// Called when stack count added
    /// </summary>
    [DataField]
    public IPathologyEffect[] AddStackEffects = Array.Empty<IPathologyEffect>();
}
