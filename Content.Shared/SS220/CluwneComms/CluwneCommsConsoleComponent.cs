// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CluwneComms
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CluwneCommsConsoleComponent : Component
    {
        [ViewVariables]
        public bool CanAnnounce;

        /// <summary>
        /// Time in seconds between announcement delays on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public TimeSpan AnnounceDelay = TimeSpan.FromSeconds(600);

        /// <summary>
        /// Time in seconds of announcement cooldown when a new console is created on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public TimeSpan InitialAnnounceDelay = TimeSpan.FromSeconds(600);

        /// <summary>
        /// Remaining cooldown between making announcements.
        /// </summary>
        [ViewVariables]
        public TimeSpan? AnnouncementCooldownRemaining;

        [ViewVariables]
        public bool CanAlert;

        /// <summary>
        /// Time in seconds between alerts delays on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public TimeSpan AlertDelay = TimeSpan.FromSeconds(1200);

        /// <summary>
        /// Time in seconds of alert cooldown when a new console is created on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public TimeSpan InitialAlertDelay = TimeSpan.FromSeconds(1200);

        /// <summary>
        /// Remaining cooldown between making funny codes
        /// </summary>
        [ViewVariables]
        public TimeSpan? AlertCooldownRemaining;

        /// <summary>
        /// Announce sound file path
        /// </summary>
        [DataField]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/SS220/Announcements/cluwne_comm_announce.ogg");

        /// <summary>
        /// Sound when on of required fields is empty
        /// </summary>
        [DataField]
        public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/SS220/Machines/CluwneComm/ui_cancel.ogg");

        /// <summary>
        /// Sound when on of required fields is empty
        /// </summary>
        [DataField]
        public SoundSpecifier BoomFailSound = new SoundPathSpecifier("/Audio/SS220/Machines/CluwneComm/boom_button_fail.ogg");

        /// <summary>
        /// Console title
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
        /// List of alerts
        /// </summary>
        [ViewVariables]
        public Dictionary<string, MemelertLevelPrototype> LevelsDict = new();

        /// <summary>
        /// Boom variables idk what they are
        /// Just made them instakill for memes
        /// </summary>
        [DataField]
        public float ExplosionTotalIntensity = 250f;

        [DataField]
        public float ExplosionSlope = 10f;

        [DataField]
        public float ExplosionMaxTileIntensity = 50f;

        [DataField]
        public float ExplosionProbability = 0.1f;
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleInterfaceState(bool canAnnounce, bool canAlert, List<string>? alertLevels, TimeSpan? announcementCooldownRemaining, TimeSpan? alertCooldownRemaining) : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce = canAnnounce;
        public readonly bool CanAlert = canAlert;
        public List<string>? AlertLevels = alertLevels;
        public TimeSpan? AnnouncementCooldownRemaining = announcementCooldownRemaining;
        public TimeSpan? AlertCooldownRemaining = alertCooldownRemaining;
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleAnnounceMessage(string message) : BoundUserInterfaceMessage
    {
        public readonly string Message = message;
    }

    /// <summary>
    ///     Sends a memelert info
    /// </summary>
    /// <param name="alert">Name of the alert sent</param>
    /// <param name="message">The text that will be displayed as a station announcement without TTS</param>
    /// <param name="instruntions">Text sent as station news to PDA</param>
    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleAlertMessage(string alert, string message, string instruntions) : BoundUserInterfaceMessage
    {
        public readonly string Alert = alert;
        public readonly string Message = message;
        public readonly string Instruntions = instruntions;
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleBoomMessage() : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum CluwneCommsConsoleUiKey : byte
    {
        Key
    }
}
