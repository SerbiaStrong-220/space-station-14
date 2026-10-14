// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.Silicons.StationAi.CameraMap;

/// <summary>
///     Lets a station AI open the surveillance network map and move its eye to a camera or beacon.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true), AutoGenerateComponentPause]
[Access(typeof(SharedStationAiCameraMapSystem))]
public sealed partial class StationAiCameraMapComponent : Component
{
    /// <summary>
    ///     Delay between two jumps through the map.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     When the next jump is allowed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextJump;

    /// <summary>
    ///     How often an open map is refreshed for changes that raise no event.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     When an open map is refreshed next.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;
}
