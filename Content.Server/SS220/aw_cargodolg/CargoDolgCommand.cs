using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Cargo.Systems;
using Content.Server.Cargo.Components;

namespace Content.Server.Cargo.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class CargoMoneyCommand : IConsoleCommand
    {
        [Dependency] private readonly CargoSystem _cargoSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "cargomoney";
        public string Description => "Turns an entity into a ghost role.";
        public string Help => $"Usage: {Command} <set || add || rem> <amount>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 3)
            {
                bool bSet = false;

                int.TryParse(args[2], out var toAdd);

                if (toAdd > 0)
                {
                    switch(args[1])
                    {
                        case "set":
                            bSet = true;
                            break;
                        case "add":
                            break;
                        case "rem":
                            toAdd = -toAdd;
                            break;
                        case null:
                            goto invalidArgs;
                    }

                    ProccessMoney(shell, toAdd, bSet);
                }
            }
        invalidArgs:
            shell.WriteLine("Expected invalid arguments!");
        }

        private void ProccessMoney(IConsoleShell shell, int money, bool bSet)
        {
            var components = _entityManager.EntityQuery<StationBankAccountComponent>();
            var bankComponent = components.GetEnumerator().Current;
            var owner = bankComponent.Owner;

            int currentMoney = bankComponent.Balance;

            _cargoSystem.UpdateBankAccount(owner, bankComponent, -currentMoney);
            _cargoSystem.UpdateBankAccount(owner, bankComponent, bSet ? currentMoney : currentMoney + money);

            shell.WriteLine($"Successfully changed cargo's money to {bankComponent.Balance}");
        }
    }
}
