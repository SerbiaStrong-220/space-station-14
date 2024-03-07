// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CluwneComms;

[RegisterComponent]
public sealed partial class CluwneCommsComponent : Component
{
    [Virtual]
    public partial class SharedCluwneCommsConsoleComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce = true;
        public float AnncounceDelay;
        public readonly TimeSpan? ExpectedCountdownEnd;
        public readonly bool CountdownStarted;
        public List<string>? AlertLevels;
        public string CurrentAlert;
        public float CurrentAlertDelay;

        public CluwneCommsConsoleInterfaceState(bool canAnnounce, float anncounceDelay, List<string>? alertLevels, string currentAlert, float currentAlertDelay, TimeSpan? expectedCountdownEnd = null)
        {
            CanAnnounce = canAnnounce;
            AnncounceDelay = anncounceDelay;
            ExpectedCountdownEnd = expectedCountdownEnd;
            CountdownStarted = expectedCountdownEnd != null;
            AlertLevels = alertLevels;
            CurrentAlert = currentAlert;
            CurrentAlertDelay = currentAlertDelay;
        }
    }


    [Serializable, NetSerializable]
    public sealed class CluwneCommsConsoleAnnounceMessage : BoundUserInterfaceMessage
    {
        public readonly string Message;

        public CluwneCommsConsoleAnnounceMessage(string message)
        {
            Message = message;
        }
    }


    [Serializable, NetSerializable]
    public enum CluwneCommsConsoleUiKey : byte
    {
        Key
    }
}
