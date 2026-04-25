// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Economy;

[Serializable, NetSerializable]
public sealed class EconomyATMUiState : BoundUserInterfaceState
{
    public CardStateEnum CardState = CardStateEnum.Default;
    public string InfoMessage = string.Empty;
    public string ErrorMessage = string.Empty;
    public BankAccount BankAccount = new();
    public string PinInput = string.Empty;
    public bool Emagged = false;
    public bool UnemployedAlert = false;
}

[Serializable, NetSerializable]
public sealed class EconomyATMKeypadMessage(int value) : BoundUserInterfaceMessage
{
    public int Value = value;
}

[Serializable, NetSerializable]
public sealed class EconomyATMKeypadClearMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class EconomyATMKeypadEnterMessage(int amount) : BoundUserInterfaceMessage
{
    public int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class EconomyATMBankAccountLinkMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class EconomyATMBankAccountCreateMessage : BoundUserInterfaceMessage
{
}
