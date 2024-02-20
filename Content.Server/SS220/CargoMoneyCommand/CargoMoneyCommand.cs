// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Created special for SS200 with love by Alan Wake (https://github.com/aw-c)

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Cargo.Systems;
using Content.Server.Cargo.Components;
using System.Linq;
using System.Collections.Generic;

namespace Content.Server.Cargo.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class CargoMoneyCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "cargomoney";
        public string Description => "Grant access to manipulate cargo's money.";
        public string Help => $"Usage: {Command} <set || add || rem> <amount>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 1 && args[0] == "getall")
            {
                PrintCargoStationsInfo(shell);
                return;
            }
            if (args.Length == 3)
            {
                bool bSet = false;

                if (int.TryParse(args[2], out var toAdd))
                {
                    switch (args[0])
                    {
                        case "set":
                            bSet = true;
                            break;
                        case "add":
                            break;
                        case "rem":
                            toAdd = -toAdd;
                            break;
                        default:
                            goto invalidArgs;
                    }

                    ProccessMoney(shell, toAdd, bSet, args[1]);
                    return;
                }
            }
        invalidArgs:
            shell.WriteLine("Expected invalid arguments!");
        }
        private void PrintCargoStationsInfo(IConsoleShell shell)
        {
            var bankQuery = _entityManager.EntityQueryEnumerator<StationBankAccountComponent>();

            while (bankQuery.MoveNext(out var uid, out var bankComp))
            {
                shell.WriteLine($"BankEntity: {uid.Id}, Values: {bankComp.Balance}");
            }
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            CompletionResult res = CompletionResult.Empty;
            switch (args.Length)
            {
                case 1:
                    res = CompletionResult.FromHint("set || add || rem || getall");
                    break;
                case 2:
                    res = CompletionResult.FromHint("EntityUid");
                    break;
                case 3:
                    res = CompletionResult.FromHint("amount");
                    break;
            }

            return res;
        }

        private void ProccessMoney(IConsoleShell shell, int money, bool bSet, string station)
        {
            if (EntityUid.TryParse(station, out var bankEnt) && _entityManager.TryGetComponent<StationBankAccountComponent>(bankEnt, out var bankComponent))
            {
                var cargoSystem = _entitySystemManager.GetEntitySystem<CargoSystem>();


                var currentMoney = bankComponent.Balance;

                cargoSystem.UpdateBankAccount(bankEnt, bankComponent, -currentMoney);
                cargoSystem.UpdateBankAccount(bankEnt, bankComponent, bSet ? money : currentMoney + money);

                shell.WriteLine($"Successfully changed EntityUid {station} cargo's money to {bankComponent.Balance}");
                
                return;
            }
            shell.WriteError("Expected invalid EntityUid!");
        }
    }
}
