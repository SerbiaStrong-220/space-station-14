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
///     Sends information about the completed interaction with the teleporter
/// </summary>
/// <param name="Target">Teleported entity</param>
/// <param name="User">Something that activated the interaction</param>
[ByRefEvent, Serializable]
public record struct TeleportTargetEvent(EntityUid Target, EntityUid User);

/// <summary>
///     Sends information to the teleporter itself that the target entity has been teleported for further postinteractions
/// </summary>
/// <param name="Target"></param>
[ByRefEvent, Serializable]
public record struct TargetTeleportedEvent(EntityUid Target);
