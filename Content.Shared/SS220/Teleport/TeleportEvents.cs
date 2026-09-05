// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Teleport;

/// <summary>
///     Notifies the teleporter immediately before moving the target.
///     Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity being teleported.</param>
[ByRefEvent, Serializable]
public record struct BeforeTeleportEvent(EntityUid Target);

/// <summary>
///     Requests that a teleport implementation move the target.
///     Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity being teleported.</param>
/// <param name="User">The entity that activated the teleporter.</param>
[ByRefEvent, Serializable]
public record struct TeleportRequestEvent(EntityUid Target, EntityUid User)
{
    /// <summary>
    ///     Whether a teleport implementation has successfully handled this request.
    /// </summary>
    public bool Handled;
}

/// <summary>
///     Requests teleportation of an observer without raising the regular teleport lifecycle events.
///     Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The observer being teleported.</param>
[ByRefEvent, Serializable]
public record struct GhostTeleportRequestEvent(EntityUid Target)
{
    /// <summary>
    ///     Whether a teleport implementation has successfully handled this request.
    /// </summary>
    public bool Handled;
}

/// <summary>
///     Notifies the teleporter that the target has been teleported.
///     Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity that was teleported.</param>
[ByRefEvent, Serializable]
public record struct TargetTeleportedEvent(EntityUid Target);

/// <summary>
///     Notifies the target that it has been teleported.
///     Raised on the teleported entity.
/// </summary>
/// <param name="Teleporter">The entity that performed the teleportation.</param>
[ByRefEvent, Serializable]
public record struct TeleportedEvent(EntityUid Teleporter);

/// <summary>
///     Checks whether the teleporter can be used.
///     Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity that will be teleported.</param>
/// <param name="User">The entity that activates the teleporter.</param>
/// <param name="Cancelled">Whether the teleporter use has been prevented.</param>
[ByRefEvent, Serializable]
public record struct TeleportUseAttemptEvent(EntityUid Target, EntityUid User, bool Cancelled = false);
