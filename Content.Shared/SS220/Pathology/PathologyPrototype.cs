// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory;
using Content.Shared.SS220.Surgery;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed partial class PathologyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    // It kinda useless for some of them...
    [DataField]
    public SlotFlags? ArmorSlotFlags = null;

    [DataField]
    public SurgeryPuppetPart? TargetZone;

    [DataField]
    public LocId? OperationRecommendation;

    [DataField]
    public LocId? ExamineDescription;

    [DataField]
    public bool IsSevere;

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
    public int MaxStackCount = SharedPathologySystem.OneStack;

    [DataField]
    public HashSet<EntProtoId> StatusEffects = new();

    [DataField]
    public ProtoId<TraitPrototype>? Trait;

    /// <summary>
    /// Multiplier applied to DoAfter delays while this pathology level is active.
    /// </summary>
    [DataField]
    public float DoAfterDelayModifier = 1f;

    /// <summary>
    /// Additional gun spread angle applied when firing with the injured hand (or any wielded gun).
    /// </summary>
    [DataField]
    public Angle GunSpreadPenalty = Angle.Zero;

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
