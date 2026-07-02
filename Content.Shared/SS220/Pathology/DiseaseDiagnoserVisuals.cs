// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Appearance keys the server sets; a GenericVisualizer maps them to sprite layers in YAML.
/// On/off is not here — the power system already publishes <c>PowerDeviceVisuals.Powered</c>.
/// </summary>
[Serializable, NetSerializable]
public enum DiseaseDiagnoserVisuals : byte
{
    Running,
    Vial,
    Buffer,
}

/// <summary>What kind of vial sits in the slot (the vial overlay layer).</summary>
[Serializable, NetSerializable]
public enum DiseaseDiagnoserVial : byte
{
    None,
    Blood,
    Empty,
    Mutagen,
}

[Serializable, NetSerializable]
public enum DiseaseDiagnoserVisualLayers : byte
{
    Powered,
    Running,
    Vial,
    Buffer,
}
