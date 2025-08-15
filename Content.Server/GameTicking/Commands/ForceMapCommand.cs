using System.Data;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Host)] // SS220-map-force-admin-flag-change
    public sealed class ForceMapCommand : LocalizedCommands
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IGameMapManager _gameMapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!; // SS220 additional command log

        public override string Command => "forcemap";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString(Loc.GetString($"shell-need-exactly-one-argument")));
                return;
            }

            var name = args[0];

            // An empty string clears the forced map
            if (!string.IsNullOrEmpty(name) && !_gameMapManager.CheckMapExists(name))
            {
                shell.WriteLine(Loc.GetString("cmd-forcemap-map-not-found", ("map", name)));
                return;
            }

            _configurationManager.SetCVar(CCVars.GameMap, name);

            if (string.IsNullOrEmpty(name))
                shell.WriteLine(Loc.GetString("cmd-forcemap-cleared"));
            else
                shell.WriteLine(Loc.GetString("cmd-forcemap-success", ("map", name)));

            LogAdminAction(shell, name); // SS220 admin action log
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = _prototypeManager
                    .EnumeratePrototypes<GameMapPrototype>()
                    .Select(p => new CompletionOption(p.ID, p.MapName))
                    .OrderBy(p => p.Value);

                return CompletionResult.FromHintOptions(options, Loc.GetString($"cmd-forcemap-hint"));
            }

            return CompletionResult.Empty;
        }

        //SS220 admin action log start
        private void LogAdminAction(IConsoleShell shell, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (shell.Player is { } player)
                {
                    _adminLogger.Add(LogType.AdminCommand, $"{player} cleared the forced map setting");
                }
                else
                {
                    _adminLogger.Add(LogType.EventStarted, $"Unknown cleared the forced map setting");
                }
            }
            else
            {
                if (shell.Player is { } player)
                {
                    _adminLogger.Add(LogType.AdminCommand, $"{player} forced the game to start with map [{name}] next round");
                }
                else
                {
                    _adminLogger.Add(LogType.EventStarted, $"Unknown forced the game to start with map [{name}] next round");
                }
            }
        }
        //SS220 admin action log end
    }
}
