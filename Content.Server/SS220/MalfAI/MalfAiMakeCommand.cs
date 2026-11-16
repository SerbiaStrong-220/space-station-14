// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.MalfAI;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.SS220.MalfAI;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class MalfAiMakeCommand : LocalizedEntityCommands
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private MalfAiRuleSystem _rules = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;

    private static readonly LocId NoEntity = "cmd-malfai_make-no-entity";
    private static readonly LocId NotStationAi = "cmd-malfai_make-not-station-ai";
    private static readonly LocId NoMind = "cmd-malfai_make-no-mind";
    private static readonly LocId Disabled = "cmd-malfai_make-disabled";
    private static readonly LocId Already = "cmd-malfai_make-already";
    private static readonly LocId Failed = "cmd-malfai_make-failed";
    private static readonly LocId Done = "cmd-malfai_make-done";

    public override string Command => "malfai_make";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        var name = session.Name;

        var attached = session.AttachedEntity;
        if (attached == null)
        {
            shell.WriteError(Loc.GetString(NoEntity, ("player", name)));
            return;
        }

        if (!_stationAi.TryGetCore(attached.Value, out _))
        {
            shell.WriteError(Loc.GetString(NotStationAi, ("player", name)));
            return;
        }

        if (!_mind.TryGetMind(session, out var mindId, out var mind))
        {
            shell.WriteError(Loc.GetString(NoMind, ("player", name)));
            return;
        }

        if (!_cfg.GetCVar(CCVars220.MalfAiEnabled))
        {
            shell.WriteError(Loc.GetString(Disabled));
            return;
        }

        if (_roles.MindHasRole<MalfAiRoleComponent>(mindId))
        {
            shell.WriteError(Loc.GetString(Already, ("player", name)));
            return;
        }

        if (!_rules.AssignMalf((mindId, mind)))
        {
            shell.WriteError(Loc.GetString(Failed, ("player", name)));
            return;
        }

        _admin.Add(LogType.Action, LogImpact.High,
            $"{shell.Player?.Name ?? "An administrator"} assigned Malfunctioning AI to {name}.");
        shell.WriteLine(Loc.GetString(Done, ("player", name)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromOptions(CompletionHelper.SessionNames(true, _players))
            : CompletionResult.Empty;
    }
}
