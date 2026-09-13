// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Teleport;

/// <summary>
///     Sends information to the entity that it was teleported.
/// </summary>
/// <param name="Teleporter">The entity that performed teleportation</param>
[ByRefEvent, Serializable]
public record struct AfterTeleportedEvent(EntityUid Teleporter);
