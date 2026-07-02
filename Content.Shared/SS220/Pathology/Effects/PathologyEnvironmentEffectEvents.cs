// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

/// <summary>Raised on a host by PathologyTemperatureEffect.</summary>
[ByRefEvent]
public record struct PathologyTemperatureEffectEvent(float Temperature);

/// <summary>Raised on a host by PathologyIgniteEffect</summary>
[ByRefEvent]
public record struct PathologyIgniteEffectEvent(float FireStacks, float Chance);
