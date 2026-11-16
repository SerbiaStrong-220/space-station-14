// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CrewManifest;

/// <summary>
///     An entry for a synthetic (borg or station AI) in the crew manifest.
/// </summary>
[Serializable, NetSerializable]
public sealed class SyntheticManifestEntry(
    string name,
    string chassisName,
    ProtoId<JobIconPrototype>? jobIcon,
    bool isAi)
{
    public string Name { get; } = name;

    /// <summary>
    ///     Localized chassis name, or the station AI label for AI entries.
    /// </summary>
    public string ChassisName { get; } = chassisName;

    /// <summary>
    ///     Job icon prototype displayed for this synthetic.
    /// </summary>
    public ProtoId<JobIconPrototype>? JobIcon { get; } = jobIcon;

    /// <summary>
    ///     Whether this entry is the station AI. Used to format the chassis label.
    /// </summary>
    public bool IsAi { get; } = isAi;
}
