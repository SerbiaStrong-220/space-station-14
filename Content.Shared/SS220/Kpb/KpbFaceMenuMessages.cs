using Content.Shared.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Kpb;

[Serializable, NetSerializable]
public sealed class KpbFaceSelectMessage : BoundUserInterfaceMessage
{
    public readonly string State;
    public KpbFaceSelectMessage(string state)
    {
        State = state;
    }
}

[Serializable, NetSerializable]
public sealed class KpbFaceBuiState : BoundUserInterfaceState
{
    public readonly string Profile;
    public readonly string Selected;
    public KpbFaceBuiState(string profile, string selected)
    {
        Profile = profile;
        Selected = selected;
    }
}

[NetSerializable, Serializable]
public enum KpbFaceUiKey : byte
{
    Face
}
