// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Virology;

[Serializable, NetSerializable]
public enum VirusDiagnoserVisuals : byte
{
    Running,
    Vial,
    Buffer,
}

/// <summary>What kind of vial sits in slot (vial overlay layer).</summary>
[Serializable, NetSerializable]
public enum VirusDiagnoserVial : byte
{
    None,
    Blood,
    Empty,
    Mutagen,
}

[Serializable, NetSerializable]
public enum VirusDiagnoserVisualLayers : byte
{
    Powered,
    Running,
    Vial,
    Buffer,
}
