// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Created special for SS200 with love by Alan Wake (https://github.com/aw-c), refactored by Kirus59 (https://github.com/Kirus59)

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Components;

namespace Content.Server.SS220.CargoMoneyCommand
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class CargoMoneyCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "cargomoney";
        public string Description => "Grant access to manipulate cargo's money.";
        public string Help => $"Usage: {Command} <set || add || rem> <value>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 1 && args[0] == "getall")
            {
                PrintCargoStationsInfo(shell);
                return;
            }

            if (args.Length != 4)
            {
                shell.WriteLine("Expected invalid arguments!");
                return;
            }

            if (!EntityUid.TryParse(args[1], out var bankUid))
            {
                shell.WriteLine($"Doesn't found entity with id: {args[1]}");
                return;
            }

            var account = args[2];
            if (!int.TryParse(args[3], out var value))
            {
                shell.WriteLine($"Failed to get int value from {args[3]}");
                return;
            }

            CargoMoneyCommandOperations operation;
            switch (args[0])
            {
                case "set":
                    operation = CargoMoneyCommandOperations.Set;
                    break;
                case "add":
                    operation = CargoMoneyCommandOperations.Add;
                    break;
                case "rem":
                    operation = CargoMoneyCommandOperations.Remove;
                    break;
                default:
                    shell.WriteLine($"Expected invalid operation: {args[0]}");
                    return;
            }

            ProccessMoney(shell, bankUid, account, operation, value);
            return;
        }

        private void PrintCargoStationsInfo(IConsoleShell shell)
        {
            var bankQuery = _entityManager.EntityQueryEnumerator<StationBankAccountComponent>();

            while (bankQuery.MoveNext(out var uid, out var bankComp))
            {
                shell.WriteLine($"\nBankUid: {uid.Id}");
                foreach (var (account, balance) in bankComp.Accounts)
                {
                    shell.WriteLine($"Account: {account.Id}, Balance: {balance}");
                }
            }
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            var res = CompletionResult.Empty;
            switch (args.Length)
            {
                case 1:
                    res = CompletionResult.FromHint("set || add || rem || getall");
                    break;
                case 2:
                    res = CompletionResult.FromHint("BankUid");
                    break;
                case 3:
                    res = CompletionResult.FromHint("Account");
                    break;
                case 4:
                    res = CompletionResult.FromHint("value");
                    break;
            }

            return res;
        }

        private void ProccessMoney(IConsoleShell shell, EntityUid bank, string account, CargoMoneyCommandOperations operation, int value)
        {
            if (!_entityManager.TryGetComponent<StationBankAccountComponent>(bank, out var bankComp))
            {
                shell.WriteLine($"Entity with id {bank} is not a bank");
                return;
            }

            var cargoSystem = _entityManager.System<CargoSystem>();
            if (!cargoSystem.BankHasAccount((bank, bankComp), account))
            {
                shell.WriteLine($"Bank with id {bank} doesn't have a \"{account}\" account");
                return;
            }

            switch (operation)
            {
                case CargoMoneyCommandOperations.Set:
                    cargoSystem.SetBankAccountBalance((bank, bankComp), value, account);
                    break;

                case CargoMoneyCommandOperations.Add:
                    cargoSystem.UpdateBankAccount((bank, bankComp), value, account);
                    break;

                case CargoMoneyCommandOperations.Remove:
                    cargoSystem.UpdateBankAccount((bank, bankComp), -value, account);
                    break;
            }

            var curBalance = bankComp.Accounts[account];
            shell.WriteLine($"Successfully changed balance of {account} account in {bank} bank to {curBalance}");
        }
    }
}

public enum CargoMoneyCommandOperations : byte
{
    Set,
    Add,
    Remove
}
