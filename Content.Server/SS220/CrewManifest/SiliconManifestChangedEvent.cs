// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.CrewManifest;

/// <summary>
///     Raised when a synthetic (borg or station AI) is added to or removed from a station's crew manifest.
/// </summary>
[ByRefEvent]
public readonly record struct SiliconManifestChangedEvent(EntityUid Station);

/// <summary>
///     Raised on a station AI core when the AI held inside it changes.
/// </summary>
[ByRefEvent]
public readonly record struct SiliconManifestCoreChangedEvent;

