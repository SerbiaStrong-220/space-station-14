// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CluwneCommunications;

[RegisterComponent]
public sealed partial class CluwneCommunicationsConsoleComponent : Component
{
    [ViewVariables]
    [DataField]
    public bool CanAnnounce;

    [ViewVariables]
    [DataField]
    public bool CanAlert;

    public string AlertLevel = "Unknown";

    /// <summary>
    /// Remaining cooldown between making announcements.
    /// </summary>
    [ViewVariables]
    [DataField]
    public TimeSpan? AnnouncementCooldownRemaining;

    [ViewVariables]
    [DataField]
    public TimeSpan? AlertCooldownRemaining;

    /// <summary>
    /// Time in seconds of announcement cooldown when a new console is created on a per-console basis
    /// </summary>
    [ViewVariables]
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Fluent ID for the announcement title
    /// If a Fluent ID isn't found, just uses the raw string
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public LocId Title = "cluwne-comms-console-announcement-title-station";

    /// <summary>
    /// Announcement color
    /// </summary>
    [ViewVariables]
    [DataField]
    public Color Color = Color.Gold;

    /// <summary>
    /// Announce on all grids (for nukies)
    /// </summary>
    [DataField]
    public bool Global = false;

    /// <summary>
    /// Announce sound file path
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/SS220/Announcements/cluwne_comm_announce.ogg");
}
[Serializable, NetSerializable]
public sealed class CluwneCommunicationsConsoleInterfaceState(bool canAnnounce, string currentAlert) : BoundUserInterfaceState
{
    public readonly bool CanAnnounce = canAnnounce;
    public string CurrentAlert = currentAlert;
}

[Serializable, NetSerializable]
public sealed class CluwneCommunicationsConsoleAnnounceMessage(string message) : BoundUserInterfaceMessage
{
    public readonly string Message = message;
}


[Serializable, NetSerializable]
public enum CluwneCommunicationsConsoleUiKey : byte
{
    Key
}
