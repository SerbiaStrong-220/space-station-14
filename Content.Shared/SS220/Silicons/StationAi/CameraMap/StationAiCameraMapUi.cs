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

[Serializable, NetSerializable]
public sealed class StationAiCameraMapState(NetEntity grid, List<StationAiCameraMapTarget> targets) : BoundUserInterfaceState
{
    public readonly NetEntity Grid = grid;
    public readonly List<StationAiCameraMapTarget> Targets = targets;
}

[Serializable, NetSerializable]
public sealed class StationAiCameraMapJumpMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public enum StationAiCameraMapFailure : byte
{
    CameraInactive,
    NoCameraNearby,
}

/// <summary>
///     Sent back when the server refuses a jump, so the window can tell the player why.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiCameraMapFailureMessage(StationAiCameraMapFailure failure) : BoundUserInterfaceMessage
{
    public readonly StationAiCameraMapFailure Failure = failure;
}
