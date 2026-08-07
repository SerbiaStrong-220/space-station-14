// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;


[Serializable, NetSerializable]
public sealed partial class VirusDescriptor
{
    /// <summary>Prototype this strain came from (identity/name), null for a mutant.</summary>
    public EntProtoId? Source;

    public string? Name;

    public VirusGenome Genome;

    public bool IsSupervirus;

    public VirusCure? Cure;

    public VirusTransmission? Transmission;

    /// <summary>strain's symptoms and their per-symptom snapshot.</summary>
    public List<VirusSymptomSnapshot> Symptoms = [];

    /// <summary>Remaining suppression time snapshotted while the source strain was supressed.</summary>
    public TimeSpan? SuppressedRemaining;

    public VirusDescriptor Clone()
    {
        var symptoms = new List<VirusSymptomSnapshot>(Symptoms.Count);
        foreach (var symptom in Symptoms)
            symptoms.Add(symptom.Clone());

        return new VirusDescriptor
        {
            Source = Source,
            Name = Name,
            Genome = Genome,
            IsSupervirus = IsSupervirus,
            Cure = Cure?.Clone(),
            Transmission = Transmission?.Clone(),
            Symptoms = symptoms,
            SuppressedRemaining = SuppressedRemaining,
        };
    }
}

[Serializable, NetSerializable]
public sealed partial class VirusSymptomSnapshot
{
    public ProtoId<VirusSymptomPrototype> Symptom;

    public int Stage;

    /// <summary>Whether a reveal chemistry has decoded this symptom (for diagnoser display).</summary>
    public bool Revealed;

    /// <summary>Reagent that accelerates this symptom's progression.</summary>
    public ProtoId<ReagentPrototype>? Accelerant;

    public VirusSymptomSnapshot Clone() => new()
    {
        Symptom = Symptom,
        Stage = Stage,
        Revealed = Revealed,
        Accelerant = Accelerant,
    };
}
