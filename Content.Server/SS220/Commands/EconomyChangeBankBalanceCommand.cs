// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.SS220.Economy;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server.SS220.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class EconomyChangeBankBalanceCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public string Command => "economy_changebalance";

    public string Description => Loc.GetString("cmd-economy_changebalance-desc");

    public string Help => "economy_changebalance <AccountId> <Amount>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2 || string.IsNullOrEmpty(args[0]))
        {
            shell.WriteLine("Wrong number of arguments");
            return;
        }

        if (!int.TryParse(args[0], out var accountId))
        {
            shell.WriteLine("AccountId should be a number.");
            return;
        }

        if (!int.TryParse(args[1], out var amount))
        {
            shell.WriteLine("Amount should be a number.");
            return;
        }

        var bankCardSystem = _entityManager.System<EconomyBankCardSystem>();

        if (!bankCardSystem.TryGetAccount(accountId, out var account))
        {
            shell.WriteLine("Account not found");
            return;
        }

        var oldBalance = account.Balance;

        bankCardSystem.TryChangeBalance(accountId, amount);

        var message = $"Account {accountId}\nBalance changed from {oldBalance} to {amount}";

        shell.WriteLine(message);

        _adminLogger.Add(LogType.AdminCommands,
            LogImpact.Medium,
            $"{shell.Player!.Name} ({shell.Player!.UserId}) changed bank balance of {accountId} from {oldBalance} to {amount}"
            );
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var bankCardSystem = _entityManager.System<EconomyBankCardSystem>();

            var bankAccounts = bankCardSystem.GetBankAccounts();

            string[] options = [""];

            if (bankAccounts is not null)
                options = [.. bankAccounts.Select(c => c.AccountId.ToString()).OrderBy(c => c)];

            return CompletionResult.FromHintOptions(options, "AccountId");
        }

        return CompletionResult.Empty;
    }
}
