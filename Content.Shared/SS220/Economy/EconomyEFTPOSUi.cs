// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Economy;

[Serializable, NetSerializable]
public sealed class EconomyEFTPOSUiState : BoundUserInterfaceState
{
    public bool Locked;
    public int Amount;
    public int OwnerBankAccountId;
    public string OwnerName = string.Empty;
    public int PayerBankAccountId;
    public string PayerPinInput = string.Empty;
    public bool PrintReceipt;
}

[Serializable, NetSerializable]
public sealed class EconomyEFTPOSPaymentMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class EconomyEFTPOSPrintReceiptMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class EconomyEFTPOSKeypadMessage(int value) : BoundUserInterfaceMessage
{
    public int Value = value;
}

[Serializable, NetSerializable]
public sealed class EconomyEFTPOSKeypadClearMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class EconomyEFTPOSKeypadEnterMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class EconomyEFTPOSLockMessage(int amount) : BoundUserInterfaceMessage
{
    public int Amount = amount;
}
