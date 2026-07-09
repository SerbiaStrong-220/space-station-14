// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;

[Serializable, NetSerializable]
public enum VirologyMachineStatus : byte
{
    /// <summary>No sample inserted.</summary>
    NoSample,

    /// <summary>Sample inserted, idle, not scanned yet.</summary>
    Ready,

    /// <summary>Scan in progress.</summary>
    Scanning,

    /// <summary>Report print in progress.</summary>
    Printing,

    /// <summary>Sample inserted, idle, scan result available.</summary>
    Result,
}
