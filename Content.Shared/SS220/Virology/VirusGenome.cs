// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;

/// <summary>Genetic base of a strain.</summary>
[Serializable, NetSerializable]
public enum VirusGenome : byte
{
    Rna,
    Dna,
}
