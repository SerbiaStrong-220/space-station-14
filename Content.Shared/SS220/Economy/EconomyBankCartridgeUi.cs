// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Economy;

[Serializable, NetSerializable]
public sealed class EconomyBankingCartridgeUiState : BoundUserInterfaceState
{
    public CardStateEnum CardState;
    public int AccountId;
    public string OwnerName = string.Empty;
    public int Balance;
}

[Serializable, NetSerializable]
public enum CardStateEnum : byte
{
    Default = 0,
    Absent = 1,
    Invalid = 2,
    Valid = 3
}
