// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Administration;
using Content.Server.SS220.Economy;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.SS220.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class EconomyGetDetailsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public string Command => "economy_getdetails";

    public string Description => Loc.GetString("cmd-economy_getdetails-desc");

    public string Help => "economy_getdetails <AccountId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || string.IsNullOrEmpty(args[0]))
        {
            shell.WriteLine("Wrong number of arguments");
            return;
        }

        if (!int.TryParse(args[0], out var accountId))
        {
            shell.WriteLine("AccountId should be a number.");
            return;
        }

        var bankCardSystem = _entityManager.System<EconomyBankCardSystem>();

        if (!bankCardSystem.TryGetAccount(accountId, out var account))
        {
            shell.WriteLine("Account not found");
            return;
        }

        var message = $"AccountId: {account.AccountId}\nAccountPin: {account.AccountPin}\nBalance: {account.Balance}\nAccountOwnersName: {account.AccountOwnerName}";

        shell.WriteLine(message);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var bankCardSystem = _entityManager.System<EconomyBankCardSystem>();

            var options = bankCardSystem.Accounts.Select(c => c.AccountId.ToString()).OrderBy(c => c).ToArray();

            return CompletionResult.FromHintOptions(options, "AccountId");
        }

        return CompletionResult.Empty;
    }
}
