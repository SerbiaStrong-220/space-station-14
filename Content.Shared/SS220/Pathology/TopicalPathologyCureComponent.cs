// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Topical medicine (e.g. ointment): each use also cures one pathology whose level is <see cref="PathologyDefinition.CuredByTopicals"/>.
/// Works through the upstream healing of the item, so it is used up the same way.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TopicalPathologyCureComponent : Component;

/// <summary>
/// Raised on a healing item to ask whether it has something to treat on the target besides damage.
/// </summary>
[ByRefEvent]
public record struct HealingCanTreatEvent(EntityUid Target, bool CanTreat = false);

/// <summary>
/// Raised on a healing item when it was used on the target.
/// </summary>
[ByRefEvent]
public readonly record struct HealingTreatedEvent(EntityUid Target, EntityUid User);
