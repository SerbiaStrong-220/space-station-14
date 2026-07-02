// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[Serializable, NetSerializable]
public sealed partial class VirusInstance
{
    public uint Id;

    /// <summary>
    /// Prototype this virus was built from, if any. Used to avoid re-infecting a host
    /// with a virus it already carries. Null for mutated viruses.
    /// </summary>
    public ProtoId<VirusPrototype>? Source;

    /// <summary>Name set by a player. Null falls back to the prototype name.</summary>
    public string? Name;

    /// <summary>Symptoms virus currently consists of.</summary>
    public List<ProtoId<PathologyPrototype>> Symptoms = new();

    /// <summary>
    /// Cached order-independent identity (sorted symptom ids), derived from Symptoms.
    /// Reset to null whenever Symptoms changes. Not networked, each side recomputes it from the replicated Symptoms.
    /// </summary>
    [NonSerialized]
    public string? CachedIdentity;

    /// <summary>
    /// Symptoms a reveal chemistry has exposed. Only these show by name, rest read as
    /// unreadable fragments. Starts empty — a fresh strain must be decoded symptom by symptom.
    /// </summary>
    public HashSet<ProtoId<PathologyPrototype>> RevealedSymptoms = new();

    /// <summary>
    /// Per-symptom stage snapshotted when this strain was stamped onto blood, 
    /// so a drawn sample reports the stage the symptom was at. Empty for a strain
    /// that never lived on a host composed in a beaker.
    /// </summary>
    public Dictionary<ProtoId<PathologyPrototype>, int> SymptomStages = new();

    /// <summary>Genetic basis of the strain. Decides whether one or all cure reagents are needed.</summary>
    public VirusGenome Genome;

    /// <summary>
    /// True if this strain was formed by merging two viruses in a host. A supervirus can't be mutated
    /// further and needs all of its cure reagents at once.
    /// </summary>
    public bool IsSupervirus;

    /// <summary> Cure rolled for this strain at spawn/last mutation. Same for every host it spreads to.</summary>
    public VirusCure? Cure;

    /// <summary>Accelerant reagent rolled per symptom at spawn/last mutation.</summary>
    public Dictionary<ProtoId<PathologyPrototype>, ProtoId<ReagentPrototype>> Accelerants = new();

    /// <summary>How this virus spreads. Null = does not transmit on its own.</summary>
    public VirusTransmission? Transmission;

    /// <summary>While set, symptoms off, not contagious, null means active.</summary>
    public TimeSpan? SuppressedUntil;

    /// <summary>
    /// Stage each symptom was at when the strain was suppressed, so reactivation resumes there
    /// instead of restarting from stage 1. Only holds symptoms that were past stage 1.
    /// </summary>
    public Dictionary<ProtoId<PathologyPrototype>, int> SuppressedStages = new();

    /// <summary>
    /// Copies this virus for transfer to another host. The copy's id is reset and
    /// reassigned when added.
    /// </summary>
    public VirusInstance Clone() => new()
    {
        Source = Source,
        Name = Name,
        Symptoms = new(Symptoms),
        RevealedSymptoms = new(RevealedSymptoms),
        SymptomStages = new(SymptomStages),
        Genome = Genome,
        IsSupervirus = IsSupervirus,
        Cure = Cure,
        Accelerants = new(Accelerants),
        Transmission = Transmission,
        SuppressedUntil = SuppressedUntil,
        SuppressedStages = new(SuppressedStages),
    };
}
