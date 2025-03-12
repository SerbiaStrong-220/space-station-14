// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.UserInterface;
using Content.Shared.Communications;
using Content.Shared.SS220.CluwneComms;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.CluwneComms
{
    [RegisterComponent]
    public sealed partial class CluwneCommsConsoleComponent : Component
    {
        [ViewVariables]
        public bool CanAnnounce;

        /// <summary>
        /// Fluent ID for the announcement title
        /// If a Fluent ID isn't found, just uses the raw string
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(required: true)]
        public LocId Title = "comms-console-announcement-title-station";

        /// <summary>
        /// Announcement color
        /// </summary>
        [ViewVariables]
        [DataField]
        public Color Color = Color.Gold;

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

        /// <summary>
        /// Announce on all grids (for nukies)
        /// </summary>
        [DataField]
        public bool Global = false;

        /// <summary>
        /// Announce sound file path
        /// </summary>
        [DataField]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleInterfaceState(bool canAnnounce) : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce = canAnnounce;
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleAnnounceMessage(string message) : BoundUserInterfaceMessage
    {
        public readonly string Message = message;
    }

    [Serializable, NetSerializable]
    public enum CluwneCommsConsoleUiKey : byte
    {
        Key
    }
}
