// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class SpeciesBanCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IBanManager _ban = default!;

    private ISawmill? _sawmill;

    public override string Command => "speciesban";
    public override string Description => Loc.GetString("cmd-speciesban-desc");
    public override string Help => Loc.GetString("cmd-speciesban-help");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string target;
        string speciesId;
        string reason;
        uint minutes;
        var postBanInfo = true;
        if (!Enum.TryParse(_cfg.GetCVar(CCVars220.SpeciesBanDefaultSeverity), out NoteSeverity severity))
        {
            _sawmill ??= _log.GetSawmill("admin.species_ban");
            _sawmill.Warning("Species ban severity could not be parsed from config! Defaulting to medium.");
            severity = NoteSeverity.Medium;
        }

        switch (args.Length)
        {
            case 3:
                target = args[0];
                speciesId = args[1];
                reason = args[2];
                minutes = 0;
                break;
            case 4:
                target = args[0];
                speciesId = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-species-ban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }

                break;
            case 5:
                target = args[0];
                speciesId = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-species-ban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }

                if (!Enum.TryParse(args[4], ignoreCase: true, out severity))
                {
                    shell.WriteLine(Loc.GetString("cmd-species-ban-severity-parse", ("severity", args[4]), ("help", Help)));
                    return;
                }

                break;
            case 6:
                target = args[0];
                speciesId = args[1];
                reason = args[2];

                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-species-ban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }

                if (!Enum.TryParse(args[4], ignoreCase: true, out severity))
                {
                    shell.WriteLine(Loc.GetString("cmd-species-ban-severity-parse", ("severity", args[4]), ("help", Help)));
                    return;
                }

                if (!bool.TryParse(args[5], out postBanInfo))
                {
                    shell.WriteLine(Loc.GetString("cmd-ban-invalid-post-ban", ("postBan", args[5])));
                    shell.WriteLine(Help);
                    return;
                }

                break;
            default:
                shell.WriteError(Loc.GetString("cmd-species-ban-arg-count"));
                shell.WriteLine(Help);
                return;
        }

        if (!_proto.HasIndex<SpeciesPrototype>(speciesId))
        {
            shell.WriteError(Loc.GetString("cmd-species-ban-parse", ("speciesId", speciesId)));
            return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            shell.WriteError(Loc.GetString("cmd-species-ban-name-parse"));
            return;
        }

        _ban.CreateSpeciesBan(located.UserId,
            located.Username,
            shell.Player?.UserId,
            null,
            located.LastHWId,
            speciesId,
            minutes,
            severity,
            reason,
            DateTimeOffset.UtcNow,
            postBanInfo);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var durOpts = new CompletionOption[]
        {
            new("0", Loc.GetString("cmd-speciesban-hint-duration-1")),
            new("1440", Loc.GetString("cmd-speciesban-hint-duration-2")),
            new("4320", Loc.GetString("cmd-speciesban-hint-duration-3")),
            new("10080", Loc.GetString("cmd-speciesban-hint-duration-4")),
            new("20160", Loc.GetString("cmd-speciesban-hint-duration-5")),
            new("43800", Loc.GetString("cmd-speciesban-hint-duration-6")),
        };

        var severities = new CompletionOption[]
        {
            new("none", Loc.GetString("admin-note-editor-severity-none")),
            new("minor", Loc.GetString("admin-note-editor-severity-low")),
            new("medium", Loc.GetString("admin-note-editor-severity-medium")),
            new("high", Loc.GetString("admin-note-editor-severity-high")),
        };

        var postInfo = new CompletionOption[]
        {
            new("true", Loc.GetString("cmd-ban-hint-post-ban-true")),
            new("false", Loc.GetString("cmd-ban-hint-post-ban-false"))
        };

        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("cmd-speciesban-hint-1")),
            2 => CompletionResult.FromHintOptions([.. CompletionHelper.PrototypeIDs<SpeciesPrototype>()], Loc.GetString("cmd-speciesban-hint-2")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-speciesban-hint-3")),
            4 => CompletionResult.FromHintOptions(durOpts, Loc.GetString("cmd-speciesban-hint-4")),
            5 => CompletionResult.FromHintOptions(severities, Loc.GetString("cmd-speciesban-hint-5")),
            6 => CompletionResult.FromHintOptions(postInfo, Loc.GetString("cmd-ban-hint-post-ban")),
            _ => CompletionResult.Empty
        };
    }
}
