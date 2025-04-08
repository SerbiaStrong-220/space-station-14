// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.UserInterface;
using Content.Shared.Communications;
using Content.Shared.SS220.CluwneComms;
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
        public TimeSpan Delay = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Time in seconds of announcement cooldown when a new console is created on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public TimeSpan InitialDelay = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Remaining cooldown between making announcements.
        /// </summary>
        [ViewVariables]
        public TimeSpan AnnouncementCooldownRemaining;

        [ViewVariables]
        public bool CanAlert;

        /// <summary>
        /// Time in seconds between alerts delays on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public TimeSpan AlertDelay = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Remaining cooldown between making funny codes.
        /// </summary>
        [ViewVariables]
        public TimeSpan AlertCooldownRemaining;

        /// <summary>
        /// Announce sound file path
        /// </summary>
        [DataField]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");

        /// <summary>
        /// Sound when on of required fields is empty
        /// </summary>
        [DataField]
        public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/SS220/Machines/CluwneComm/ui_cancel.ogg");

        /// <summary>
        /// Console name
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
        /// Boom variables
        /// </summary>
        [DataField]
        public float TotalIntensity = 250f;

        [DataField]
        public float Slope = 10f;

        [DataField]
        public float MaxTileIntensity = 50f;
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleInterfaceState(bool canAnnounce, bool canAlert, List<string>? alertLevels) : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce = canAnnounce;
        public readonly bool CanAlert = canAlert;
        public List<string>? AlertLevels = alertLevels;
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleAnnounceMessage(string message) : BoundUserInterfaceMessage
    {
        public readonly string Message = message;
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleAlertMessage(string alert, string message, string instruntions) : BoundUserInterfaceMessage
    {
        public readonly string Alert = alert;
        public readonly string Message = message;
        public readonly string Instruntions = instruntions;
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleBoomMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum CluwneCommsConsoleUiKey : byte
    {
        Key
    }
}
