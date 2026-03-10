using Robust.Shared.Serialization;

namespace Content.Shared.SS220.BeerUpdate.MaltGrinder;

public sealed class SharedMaltGrinder
{
    public static string BeakerSlotId = "beakerSlot";

    public static string InputContainerId = "inputContainer";
}

[Serializable, NetSerializable]
public sealed class MaltGrinderStartMessage : BoundUserInterfaceMessage
{
    public MaltGrinderStartMessage()
    {
    }
}

[Serializable, NetSerializable]
public sealed class MaltGrinderEjectChamberAllMessage : BoundUserInterfaceMessage
{
    public MaltGrinderEjectChamberAllMessage()
    {
    }
}

[Serializable, NetSerializable]
public sealed class MaltGrinderWorkStartedMessage : BoundUserInterfaceMessage
{
    public MaltGrinderWorkStartedMessage()
    {
    }
}

[Serializable, NetSerializable]
public enum MaltGrinderUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class MaltGrinderInterfaceState : BoundUserInterfaceState
{
    public bool IsProcessing;
    public bool HasBeaker;
    public bool ChamberFull;
    public int ChamberCount;

    public MaltGrinderInterfaceState(bool isProcessing, bool hasBeaker, bool chamberfull, int chamberCount)
    {
        IsProcessing = isProcessing;
        HasBeaker = hasBeaker;
        ChamberFull = chamberfull;
        ChamberCount = chamberCount;
    }
}

[Serializable, NetSerializable]
public enum MaltGrinderVisualState : byte
{
    On
}

[Serializable, NetSerializable]
public enum MaltGrinderVisualLayers : byte
{
    Base,
    On
}
