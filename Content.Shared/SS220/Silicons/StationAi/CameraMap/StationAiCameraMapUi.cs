// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Silicons.StationAi.CameraMap;

[Serializable, NetSerializable]
public enum StationAiCameraMapUiKey : byte
{
    Key,
}

/// <summary>
///     A camera or navigation beacon the AI can see on the map.
/// </summary>
[Serializable, NetSerializable]
public sealed record StationAiCameraMapTarget(NetEntity Entity, Vector2 Position, string Name, bool IsCamera, bool Active);

/// <summary>
///     A crew member whose suit sensors are on, as the crew monitor sees them.
/// </summary>
/// <param name="HealthLevel">Damage in quarters of the crit threshold (0-4, 5 is critical); null without vitals.</param>
/// <param name="Located">Whether the sensors report coordinates on the AI's grid.</param>
[Serializable, NetSerializable]
public sealed record StationAiCameraMapCrew(NetEntity Sensor, string Name, string Job, string JobIcon, bool Alive,
    int? HealthLevel, bool Located);

[Serializable, NetSerializable]
public sealed class StationAiCameraMapState(
    NetEntity grid,
    List<StationAiCameraMapTarget> targets,
    List<StationAiCameraMapCrew> crew,
    Dictionary<NetEntity, Vector2> crewPositions) : BoundUserInterfaceState
{
    public readonly NetEntity Grid = grid;
    public readonly List<StationAiCameraMapTarget> Targets = targets;
    public readonly List<StationAiCameraMapCrew> Crew = crew;

    /// <summary>
    ///     Kept apart from <see cref="Crew"/> so movement alone does not rebuild the crew list.
    /// </summary>
    public readonly Dictionary<NetEntity, Vector2> CrewPositions = crewPositions;
}

[Serializable, NetSerializable]
public sealed class StationAiCameraMapJumpMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

/// <summary>
///     Moves the AI eye to a crew member by their suit sensor.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiCameraMapCrewJumpMessage(NetEntity sensor) : BoundUserInterfaceMessage
{
    public readonly NetEntity Sensor = sensor;
}

[Serializable, NetSerializable]
public enum StationAiCameraMapFailure : byte
{
    CameraInactive,
    NoCameraNearby,
    NoCoordinates,
    OutOfView,
}

/// <summary>
///     Sent back when the server refuses a jump, so the window can tell the player why.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiCameraMapFailureMessage(StationAiCameraMapFailure failure) : BoundUserInterfaceMessage
{
    public readonly StationAiCameraMapFailure Failure = failure;
}
