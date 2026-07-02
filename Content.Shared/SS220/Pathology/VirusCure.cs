// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[Serializable, NetSerializable]
public enum VirusGenome : byte
{
    Rna,
    Dna,
}

/// <summary>Cure rolled for a strain at spawn/mutation.</summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class VirusCure
{
    /// <summary>Reagents that cure virus (one natural + one synthesized).</summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> Reagents = new();
}
