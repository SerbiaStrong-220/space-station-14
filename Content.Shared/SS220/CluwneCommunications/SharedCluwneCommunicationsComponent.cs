// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CluwneCommunications
{
    [Virtual]
    public sealed partial class SharedCluwneCommunicationsConsoleComponent : Component
    {
    }
    [Serializable, NetSerializable]
    public sealed class CluwneCommunicationsConsoleInterfaceState(bool canAnnounce) : BoundUserInterfaceState
    {
        public readonly bool CanAnnounce = canAnnounce;
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
}
