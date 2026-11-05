// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// Raised directed on an entity when any of Experience changing component added or removed or Experience needs recalculation
/// </summary>
[ByRefEvent]
public readonly record struct RecalculateEntityExperience();
