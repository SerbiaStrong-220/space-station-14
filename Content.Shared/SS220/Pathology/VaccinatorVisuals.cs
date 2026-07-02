// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

[Serializable, NetSerializable]
public enum VaccinatorVisuals : byte
{
    Running,
    Vial,
    BufferFill,
}

/// <summary>What is in vial slot.</summary>
[Serializable, NetSerializable]
public enum VaccinatorVial : byte
{
    None,
    Empty,
    Tricordrazine,
    Blood,
}

[Serializable, NetSerializable]
public enum VaccinatorVisualLayers : byte
{
    Powered,
    Running,
    Vial,
    Buffer,
}
