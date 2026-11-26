// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
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

    /// <summary>
    /// Organ this pathology lives in. It can't exist without the organ and moves together with it on extraction and insertion.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Organ;

    /// <summary>
    /// Organ whose absence this pathology represents. Added when the organ is removed and removed when it is inserted back.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? MissingOrgan;

    /// <summary>
    /// While this pathology is active, the status effects of these ones don't apply,
    /// e.g. the slowdown of a missing leg replaces the one of the missing foot it took along.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<PathologyPrototype>> OverridesStatusEffects = new();

    /// <summary>
    /// The missing organ is the whole body part of <see cref="TargetZone"/>, e.g. a severed arm, so body dolls show the zone as gone.
    /// </summary>
    [DataField]
    public bool MissingPart;

    /// <summary>
    /// If set, an organ extracted from a rotten body stores this pathology at its last level.
    /// </summary>
    [DataField]
    public bool FailsOnRot;

    /// <summary>
    /// A bone trauma, never added to boneless bodies (e.g. slime people).
    /// </summary>
    [DataField]
    public bool AffectsBones;

    [DataField]
    public PathologyDefinition[] Definition = Array.Empty<PathologyDefinition>();
}

[DataDefinition]
public sealed partial class PathologyDefinition
{
    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Replaces the pathology name while this level is active, e.g. organ damage turned into organ failure.
    /// </summary>
    [DataField]
    public LocId? Name;

    /// <summary>
    /// Replaces the pathology operation recommendation while this level is active, e.g. organ repair is possible only on failure.
    /// </summary>
    [DataField]
    public LocId? OperationRecommendation;

    [DataField]
    public LocId? ProgressPopup;

    [DataField]
    public int MaxStackCount = SharedPathologySystem.OneStack;

    /// <summary>
    /// The same pathology added again (e.g. a new burn of the zone) raises this level to the next one.
    /// </summary>
    [DataField]
    public bool WorsensOnRepeat;

    /// <summary>
    /// This level is cured by topical medicine (e.g. ointment), see <see cref="TopicalPathologyCureComponent"/>.
    /// </summary>
    [DataField]
    public bool CuredByTopicals;

    /// <summary>
    /// False for stacks that only lead to the next level (e.g. organ damage before failure), they don't make the pathology severe.
    /// </summary>
    [DataField]
    public bool StacksAreSevere = true;

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
    /// Multiplier of the gun spread when firing with the injured hand (or any wielded gun), e.g. 1.2 is +20% of the gun's own spread.
    /// </summary>
    [DataField]
    public float GunSpreadModifier = 1f;

    [DataField]
    public PathologyProgressCondition[] ProgressConditions = Array.Empty<PathologyProgressCondition>();

    /// <summary>
    /// Chance per update interval to lose one stack. Pathology is removed when the last stack is lost.
    /// </summary>
    [DataField]
    public float StackDecayChance;

    /// <summary>
    /// Inhaled gas doesn't reach the lungs, so the holder suffocates as if not breathing at all (e.g. lung failure).
    /// </summary>
    [DataField]
    public bool StopsBreathing;

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
