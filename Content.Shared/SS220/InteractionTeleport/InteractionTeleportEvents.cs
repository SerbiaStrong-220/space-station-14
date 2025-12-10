// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.InteractionTeleport;

/// <summary>
/// InteractionTeleportComponent DoAfter
/// </summary>
[Serializable, NetSerializable]
public sealed partial class InteractionTeleportDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
///     Sends information about the completed interaction to other teleport components, which are supposed to perform the teleportation
///     Raised on teleporter entity.
/// </summary>
/// <param name="Target">The entity that is currently teleporting</param>
/// <param name="User">An entity that interacts with a teleporter</param>
[ByRefEvent, Serializable]
public record struct TeleportTargetEvent(EntityUid Target, EntityUid User);

/// <summary>
///     Sends information to the teleporter itself that the target entity has been teleported for further postinteractions
/// </summary>
/// <param name="Target">The entity that was teleported</param>
[ByRefEvent, Serializable]
public record struct TargetTeleportedEvent(EntityUid Target);

/// <summary>
///     Event raised when attempting to use teleporter to check if it can be used.
///     Raised on teleporter entity.
/// </summary>
/// <param name="Target">The entity that will be teleported</param>
/// <param name="User">The entity that activates the teleporter</param>
/// <param name="Cancelled">Whether the teleporter use has been prevented</param>
[ByRefEvent, Serializable]
public record struct TeleportUseAttemptEvent(EntityUid Target, EntityUid User, bool Cancelled = false);
