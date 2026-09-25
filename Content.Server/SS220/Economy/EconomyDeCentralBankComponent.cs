// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Economy;

namespace Content.Server.SS220.Economy;

[RegisterComponent]
public sealed partial class EconomyDeCentralBankComponent : Component
{
    [ViewVariables]
    public bool IsCentralNode = false;

    [ViewVariables]
    public readonly List<BankAccount> Accounts = [];
}
