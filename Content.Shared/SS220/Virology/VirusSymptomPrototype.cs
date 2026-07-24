// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid.Prototypes;
using Content.Shared.SS220.Virology.Effects;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology;

[Prototype]
public sealed partial class VirusSymptomPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>Genome all carrying strains must share.</summary>
    [DataField]
    public VirusGenome Genome = VirusGenome.Rna;

    [DataField]
    public bool BloodBorneOnly;

    [DataField(required: true)]
    public VirusSymptomStage[] Stages = [];

    /// <summary>Per-species tuning: immunity, stage skipping, component/manifestation changes.</summary>
    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, VirusSpeciesOverride> SpeciesOverrides = [];
}

[DataDefinition]
public sealed partial class VirusSymptomStage
{
    /// <summary>Host components put while "X" stage is active.</summary>
    [DataField]
    public ComponentRegistry Components = [];

    /// <summary>Effects ticking on host while "X" stage is active - damage, bleed etc. data, not components.</summary>
    [DataField]
    public IVirusEffect[] Effects = [];

    /// <summary>Must pass to advance to next stage. Empty on the last stage.</summary>
    [DataField]
    public VirusProgressCondition[] ProgressConditions = [];

    /// <summary>How this stage shows on host (examine, emote, self message).</summary>
    [DataField]
    public VirusSymptomManifestation? Manifestation;

    /// <summary>How diagnoser reports this stage. Null = undetectable.</summary>
    [DataField]
    public VirusSymptomDetection? Detection;

    /// <summary>Chat line to the carrier when this stage begins.</summary>
    [DataField]
    public LocId? ProgressMessage;

    [DataField]
    public Color? ProgressMessageColor;
}

[DataDefinition]
public sealed partial class VirusSpeciesOverride
{
    /// <summary>Symptom never applies to this species.</summary>
    [DataField]
    public bool Immune;

    /// <summary>Stages below this index are treated as empty for this species (tajara skips sharp-hearing stage 1).</summary>
    [DataField]
    public int MinStage;

    /// <summary>If set, replaces stage's component set entirely for this species.</summary>
    [DataField]
    public ComponentRegistry? ReplaceComponents;

    /// <summary>Component names stripped from stage's set for this species (unati to cool to suffer necrosis).</summary>
    [DataField]
    public HashSet<string> RemoveComponents = [];

    /// <summary>Extra components added for this species.</summary>
    [DataField]
    public ComponentRegistry AddComponents = [];

    /// <summary>Drop stage's ticked effects for this species (unati take no necrosis damage, only chat msg).</summary>
    [DataField]
    public bool SuppressEffects;

    /// <summary>Replaces stage's manifestation for this species (flavour examine).</summary>
    [DataField]
    public VirusSymptomManifestation? ManifestationOverride;
}
