// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.EmergencyShuttleControl;

/// <summary>
///     A component that allows you to hold the evacuation shuttle call and make announcements upon activation/deactivation.
///     It also allows you to check its location at the station before activation.
/// </summary>
[RegisterComponent]
public sealed partial class EmergencyShuttleLockdownComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsActivated = default!;

    [DataField]
    public bool IsInHandActive = default!;

    /// <summary>
    ///     Enables display of location in announce.
    /// </summary>
    [DataField]
    public bool IsDisplayLocation = default!;

    /// <summary>
    ///     Enables display of coordinates in announce.
    /// </summary>
    [DataField]
    public bool IsDisplayCoordinates = default!;


    /// <summary>
    ///     Enables checking location at a station for activation. If this is false, the component can always be activated.
    /// </summary>
    [DataField]
    public bool IsOnlyInStationActive = default!;

    /// <summary>
    ///     Message above communication console, when shuttle is called during lockdown.
    /// </summary>
    [DataField]
    public LocId WarningMessage = default!;



    #region Announce

    [DataField]
    public Color AnnounceColor = default!;

    [DataField]
    public string OnActiveAudioPath = "/Audio/Misc/notice1.ogg";

    [DataField]
    public string OnDeactiveAudioPath = "/Audio/Misc/notice1.ogg";

    [DataField]
    public LocId AnnounceTitle = default!;

    /// <summary>
    ///     The body of the message in the announce if IsActivated.
    ///     If this is null, there will be no notification at all.
    /// </summary>
    [DataField]
    public LocId? OnActiveMessage = default!;

    /// <summary>
    ///     The body of the message in the announce if !IsActivated.
    ///     If this is null, there will be no notification at all.
    /// </summary>
    [DataField]
    public LocId? OnDeactiveMessage = default!;

    #endregion
}
