// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Bible;

[Serializable, NetSerializable]
public sealed class ExorcismInterfaceState : BoundUserInterfaceState
{
    public readonly int LengthMin;
    public readonly int LengthMax;

    public ExorcismInterfaceState(int lengthMin, int lengthMax)
    {
        LengthMin = lengthMin;
        LengthMax = lengthMax;
    }
}

[Serializable, NetSerializable]
public sealed class ExorcismReadMessage : BoundUserInterfaceMessage
{
    public readonly string Message;

    public ExorcismReadMessage(string message)
    {
        Message = message;
    }
}

[Serializable, NetSerializable]
public enum ExorcismUiKey
{
    Key
}
