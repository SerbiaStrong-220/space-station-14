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
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server.SS220.MalfAI;

[ToolshedCommand(Name = "malfai_make"), AdminCommand(AdminFlags.Admin)]
public sealed class MalfAiMakeCommand : ToolshedCommand
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private MindSystem? _mind;
    private SharedRoleSystem? _roles;
    private MalfAiRuleSystem? _rules;
    private SharedStationAiSystem? _stationAi;

    private static readonly LocId NoEntity = "cmd-malfai_make-no-entity";
    private static readonly LocId NotStationAi = "cmd-malfai_make-not-station-ai";
    private static readonly LocId NoMind = "cmd-malfai_make-no-mind";
    private static readonly LocId Disabled = "cmd-malfai_make-disabled";
    private static readonly LocId Already = "cmd-malfai_make-already";
    private static readonly LocId Failed = "cmd-malfai_make-failed";
    private static readonly LocId Done = "cmd-malfai_make-done";

    [CommandImplementation]
    public void Make(IInvocationContext ctx, ICommonSession session)
    {
        _mind ??= GetSys<MindSystem>();
        _roles ??= GetSys<SharedRoleSystem>();
        _rules ??= GetSys<MalfAiRuleSystem>();
        _stationAi ??= GetSys<SharedStationAiSystem>();

        var name = session.Name;

        var attached = session.AttachedEntity;
        if (attached == null)
        {
            ctx.WriteLine(Loc.GetString(NoEntity, ("player", name)));
            return;
        }

        if (!_stationAi.TryGetCore(attached.Value, out _))
        {
            ctx.WriteLine(Loc.GetString(NotStationAi, ("player", name)));
            return;
        }

        if (!_mind.TryGetMind(session, out var mindId, out var mind))
        {
            ctx.WriteLine(Loc.GetString(NoMind, ("player", name)));
            return;
        }

        if (!_cfg.GetCVar(CCVars220.MalfAiEnabled))
        {
            ctx.WriteLine(Loc.GetString(Disabled));
            return;
        }

        if (_roles.MindHasRole<MalfAiRoleComponent>(mindId))
        {
            ctx.WriteLine(Loc.GetString(Already, ("player", name)));
            return;
        }

        if (!_rules.AssignMalf((mindId, mind)))
        {
            ctx.WriteLine(Loc.GetString(Failed, ("player", name)));
            return;
        }

        _admin.Add(LogType.Action, LogImpact.High,
            $"{ctx.Session?.Name ?? "An administrator"} assigned Malfunctioning AI to {name}.");
        ctx.WriteLine(Loc.GetString(Done, ("player", name)));
    }
}
