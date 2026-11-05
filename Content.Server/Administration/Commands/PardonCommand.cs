using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class PardonCommand : LocalizedCommands
    {
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly IBanManager _banManager = default!; // SS220-make-pardon-better

        public override string Command => "pardon";

        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;

            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var banId))
            {
                shell.WriteLine(Loc.GetString($"cmd-pardon-unable-to-parse", ("id", args[0]), ("help", Help)));
                return;
            }

            var ban = await _dbManager.GetBanAsync(banId);

            if (ban == null)
            {
                shell.WriteLine($"No ban found with id {banId}");
                return;
            }

            if (ban.Unban != null)
            {
                if (ban.Unban.UnbanningAdmin != null)
                {
                    shell.WriteLine(Loc.GetString($"cmd-pardon-already-pardoned-specific",
                        ("admin", ban.Unban.UnbanningAdmin.Value),
                        ("time", ban.Unban.UnbanTime)));
                }

                else
                    shell.WriteLine(Loc.GetString($"cmd-pardon-already-pardoned"));

                return;
            }

            // SS220-make-pardon-better-begin
            // await _dbManager.AddUnbanAsync(new UnbanDef(banId, player?.UserId, DateTimeOffset.Now)); [wizden-coded]
            switch (ban.Type)
            {
                case BanType.Server:
                    await _dbManager.AddUnbanAsync(new UnbanDef(banId, player?.UserId, DateTimeOffset.Now));
                    break;

                case BanType.Role:
                    await _banManager.PardonRoleBan(banId, player?.UserId, DateTimeOffset.Now);
                    break;

                case BanType.Chat:
                    await _banManager.PardonChatsBan(banId, player?.UserId, DateTimeOffset.Now);
                    break;

                case BanType.Species:
                    await _banManager.PardonSpeciesBan(banId, player?.UserId, DateTimeOffset.Now);
                    break;

                default:
                    shell.WriteLine($"Ban with undefined type found with id {ban.Id} and type {ban.Type}!");
                    break;
            }
            // SS220-make-pardon-better-end

            shell.WriteLine(Loc.GetString($"cmd-pardon-success", ("id", banId)));
        }
    }
}
