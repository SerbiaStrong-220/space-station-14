// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Surgery;

[ByRefEvent]
public record struct GetSurgeryDelayModifiersEvent(float FlatModifier, float Multiplier);
