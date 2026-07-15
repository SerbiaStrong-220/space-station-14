using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pirates;

[Serializable, NetSerializable]
public enum PirateLootConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PirateLootConsoleState(int appraisal, int itemCount, long totalLootValue, int totalItemsSold) : BoundUserInterfaceState
{
    public readonly int Appraisal = appraisal;
    public readonly int ItemCount = itemCount;
    public readonly long TotalLootValue = totalLootValue;
    public readonly int TotalItemsSold = totalItemsSold;
}

[Serializable, NetSerializable]
public sealed class PirateLootSellMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class PirateLootAppraiseMessage : BoundUserInterfaceMessage;

