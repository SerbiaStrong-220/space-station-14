// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[Flags, Serializable, NetSerializable]
public enum VirusTransmissionVector : byte
{
    None = 0,

    /// <summary>Touch interactions: hugs, hits, grabs, pulls, handling tainted items.</summary>
    Contact = 1 << 0,

    /// <summary>Airborne spread to nearby hosts.</summary>
    Proximity = 1 << 1,
}

/// <summary>How a virus spreads.</summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class VirusTransmission
{
    /// <summary>Chance (0..1) to spread on a contact. 0 = no contact spread.</summary>
    [DataField]
    public float ContactChance;

    /// <summary>Chance (0..1) per second to transmit to each nearby host. 0 disables airborne spread.</summary>
    [DataField]
    public float ProximityChance;

    /// <summary>Radius (in tiles) of airborne spread.</summary>
    [DataField]
    public float ProximityRange = 2f;
}
