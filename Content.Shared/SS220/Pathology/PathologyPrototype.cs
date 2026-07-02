// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[Prototype]
public sealed partial class PathologyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public VirusGenome Genome = VirusGenome.Rna;

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

    /// <summary>Chat feedback sent to the carrier's chat when this stage begins.</summary>
    [DataField]
    public LocId? ProgressMessage;

    /// <summary>Colour of the <see cref="ProgressMessage"/> chat line. Null uses the default colour.</summary>
    [DataField]
    public Color? ProgressMessageColor;

    [DataField]
    public int MaxStackCount = SharedPathologySystem.DefaultMaxStack;

    [DataField]
    public HashSet<EntProtoId> StatusEffects = new();

    /// <summary>Components added to host while this stage is active and stripped when virus/stage removed.</summary>
    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public PathologyProgressCondition[] ProgressConditions = Array.Empty<PathologyProgressCondition>();

    /// <summary>How this stage shows itself on a host (examine signs, emotes)</summary>
    [DataField]
    public SymptomManifestation? Manifestation;

    /// <summary>How medical devices detect this stage. Null = undetectable by any scanner.</summary>
    [DataField]
    public SymptomDetection? Detection;

    /// <summary>Host of a listed species uses the override instead of the default.</summary>
    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, SymptomManifestation> SpeciesManifestations = new();

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
