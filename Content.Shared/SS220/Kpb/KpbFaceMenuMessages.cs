using Content.Shared.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Kpb;

[Serializable, NetSerializable]
public sealed class KpbFaceSelectMessage(string state) : BoundUserInterfaceMessage
{
    public readonly string State = state;
}

[Serializable, NetSerializable]
public sealed class KpbFaceBuiState(string profile, string selected) : BoundUserInterfaceState
{
    public readonly string Profile = profile;
    public readonly string Selected = selected;
}

[NetSerializable, Serializable]
public enum KpbFaceUiKey : byte
{
    Face
}
