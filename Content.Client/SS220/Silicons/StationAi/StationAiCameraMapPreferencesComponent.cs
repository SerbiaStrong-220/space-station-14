// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;

namespace Content.Client.SS220.Silicons.StationAi;

/// <summary>Local preferences for this AI, retained when its map window is closed.</summary>
[RegisterComponent]
public sealed partial class StationAiCameraMapPreferencesComponent : Component
{
    public readonly HashSet<NetEntity> Favorites = new();

    /// <summary>Window size and position from the last time it was closed.</summary>
    public Vector2? WindowSize;
    public Vector2? WindowPosition;
}
