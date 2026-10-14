// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Silicons.StationAi;

[RegisterComponent]
public sealed partial class StationAiCameraMapComponent : Component
{
    /// <summary>
    /// Shared so the client can draw cooldown progress.
    /// </summary>
    public static readonly TimeSpan TeleportCooldown = TimeSpan.FromSeconds(5);

    public TimeSpan NextTeleport;

    // Last state sent to the client, so unchanged periodic refreshes are skipped.
    public EntityUid? LastGrid;
    public TimeSpan LastSentTeleport;
    public List<StationAiCameraMapTarget> LastTargets = new();
    public List<StationAiCameraMapCrew> LastCrew = new();
}

[Serializable, NetSerializable]
public enum StationAiCameraMapUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class StationAiCameraMapState(NetEntity grid, List<StationAiCameraMapTarget> targets,
    List<StationAiCameraMapCrew> crew, TimeSpan nextTeleport) : BoundUserInterfaceState
{
    public readonly NetEntity Grid = grid;
    public readonly TimeSpan NextTeleport = nextTeleport;
    public readonly List<StationAiCameraMapTarget> Targets = targets;
    public readonly List<StationAiCameraMapCrew> Crew = crew;
}

/// <summary>
/// A crew member whose suit sensors are on, as the crew monitor would see them.
/// <see cref="Position"/> is only known in coordinates mode and only while they are on the AI's grid.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct StationAiCameraMapCrew(NetEntity Sensor, string Name, string Job, string JobIcon,
    bool Alive, float? Damage, Vector2? Position);

[Serializable, NetSerializable]
public enum StationAiCameraMapFailure : byte
{
    /// <summary>The crew member is in a zone no camera can see (the static on the AI overlay).</summary>
    OutOfView,
    /// <summary>The crew member's sensors do not report coordinates.</summary>
    NoCoordinates,
    /// <summary>The camera is off, unpowered or cut off from the AI.</summary>
    CameraInactive,
}

/// <summary>Server reply when a jump request is refused for a reason the player should see.</summary>
[Serializable, NetSerializable]
public sealed class StationAiCameraMapFailureMessage(StationAiCameraMapFailure failure) : BoundUserInterfaceMessage
{
    public readonly StationAiCameraMapFailure Failure = failure;
}

/// <summary>Jump to the camera nearest to a crew member that can actually see them.</summary>
[Serializable, NetSerializable]
public sealed class StationAiCameraMapCrewMessage(NetEntity sensor) : BoundUserInterfaceMessage
{
    public readonly NetEntity Sensor = sensor;
}

[Serializable, NetSerializable]
public readonly record struct StationAiCameraMapTarget(NetEntity Entity, Vector2 Position, string Name, bool IsCamera, bool Active);

/// <summary>Returns the AI view to its core, replacing the standalone "jump to core" action.</summary>
[Serializable, NetSerializable]
public sealed class StationAiCameraMapJumpToCoreMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class StationAiCameraMapTeleportMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}
