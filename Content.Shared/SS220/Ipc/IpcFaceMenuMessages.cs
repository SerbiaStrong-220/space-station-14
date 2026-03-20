// Taken from: Corvax https://github.com/space-syndicate/space-station-14

using Content.Shared.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Ipc;

[Serializable, NetSerializable]
public sealed class IpcFaceSelectMessage(string state) : BoundUserInterfaceMessage
{
    public readonly string State = state;
}

[Serializable, NetSerializable]
public sealed class IpcFaceBuiState(string profile, string selected) : BoundUserInterfaceState
{
    public readonly string Profile = profile;
    public readonly string Selected = selected;
}

[NetSerializable, Serializable]
public enum IpcFaceUiKey : byte
{
    Face
}
