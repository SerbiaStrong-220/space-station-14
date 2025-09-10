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
}
