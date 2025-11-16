// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.EmergencyShuttleControl;

/// <summary>
///     A component that allows you to hold the evacuation shuttle call and make announcements upon activation/deactivation.
///     It also allows you to check its location at the station before activation.
/// </summary>
[RegisterComponent]
public sealed partial class EmergencyShuttleLockdownComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsActive;

    /// <summary>
    ///     It allows the component to be activated at startup.
    /// </summary>
    [DataField]
    public bool IsActivatedOnStartup;

    [DataField]
    public bool IsInHandActive;

    /// <summary>
    ///     Enables display of location in announce.
    /// </summary>
    [DataField]
    public bool IsDisplayLocation;

    /// <summary>
    ///     Enables display of coordinates in announce.
    /// </summary>
    [DataField]
    public bool IsDisplayCoordinates;


    /// <summary>
    ///     Enables checking location at a station for activation. If this is false, the component can always be activated.
    /// </summary>
    [DataField]
    public bool IsOnlyInStationActive;

    /// <summary>
    ///     Message above communication console, when shuttle is called during lockdown.
    /// </summary>
    [DataField]
    public LocId WarningMessage = string.Empty;



    #region Announce

    [DataField]
    public Color AnnounceColor = Color.Red;

    [DataField]
    public SoundSpecifier ActivateSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

    [DataField]
    public SoundSpecifier OnDeactiveAudioPath = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");

    [DataField]
    public LocId AnnounceTitle = string.Empty;

    /// <summary>
    ///     The body of the message in the announce if IsActivated.
    ///     If this is null, there will be no notification at all.
    /// </summary>
    [DataField]
    public LocId? OnActiveMessage;

    /// <summary>
    ///     The body of the message in the announce if !IsActivated.
    ///     If this is null, there will be no notification at all.
    /// </summary>
    [DataField]
    public LocId? OnDeactiveMessage;

    #endregion
}
