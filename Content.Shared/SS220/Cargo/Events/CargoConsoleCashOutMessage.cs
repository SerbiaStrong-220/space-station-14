using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Cargo.Events;

/// <summary>
///     Cashout Money from station bank account.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleCashOutMessage : BoundUserInterfaceMessage
{
    public int Amount;

    public CargoConsoleCashOutMessage(int amount)
    {
        Amount = amount;
    }
}
