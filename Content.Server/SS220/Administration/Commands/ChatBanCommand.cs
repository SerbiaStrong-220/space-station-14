using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class ChatBanCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IBanManager _ban = default!;

    private ISawmill? _sawmill;

    public override string Command => "chatban";
    public override string Description => Loc.GetString("cmd-chat-ban-desc");
    public override string Help => Loc.GetString("cmd-chat-ban-help");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string target;
        BannableChats bannableChat;
        string reason;
        uint minutes;
        var postBanInfo = true;
        if (!Enum.TryParse(_cfg.GetCVar(CCVars220.ChatBanDefaultSeverity), out NoteSeverity severity))
        {
            _sawmill ??= _log.GetSawmill("admin.chat_ban");
            _sawmill.Warning("Chat ban severity could not be parsed from config! Defaulting to medium.");
            severity = NoteSeverity.Medium;
        }

        switch (args.Length)
        {
            case 3:
                target = args[0];
                if (!Enum.TryParse(args[1], out bannableChat))
                {
                    shell.WriteError(Loc.GetString("cmd-chat-ban-chat-parse", ("time", args[1]), ("help", Help)));
                    return;
                }
                reason = args[2];
                minutes = 0;
                break;

            case 4:
                target = args[0];
                if (!Enum.TryParse(args[1], out bannableChat))
                {
                    shell.WriteError(Loc.GetString("cmd-chat-ban-chat-parse", ("time", args[1]), ("help", Help)));
                    return;
                }
                reason = args[2];
                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-chat-ban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }
                break;

            case 5:
                target = args[0];
                if (!Enum.TryParse(args[1], out bannableChat))
                {
                    shell.WriteError(Loc.GetString("cmd-chat-ban-chat-parse", ("time", args[1]), ("help", Help)));
                    return;
                }
                reason = args[2];
                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-chat-ban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }
                if (!Enum.TryParse(args[4], ignoreCase: true, out severity))
                {
                    shell.WriteLine(Loc.GetString("cmd-chat-ban-severity-parse",
                        ("severity", args[4]),
                        ("help", Help)));
                    return;
                }
                break;

            case 6:
                target = args[0];
                if (!Enum.TryParse(args[1], out bannableChat))
                {
                    shell.WriteError(Loc.GetString("cmd-chat-ban-chat-parse", ("time", args[1]), ("help", Help)));
                    return;
                }
                reason = args[2];
                if (!uint.TryParse(args[3], out minutes))
                {
                    shell.WriteError(Loc.GetString("cmd-chat-ban-minutes-parse", ("time", args[3]), ("help", Help)));
                    return;
                }
                if (!Enum.TryParse(args[4], ignoreCase: true, out severity))
                {
                    shell.WriteLine(Loc.GetString("cmd-chat-ban-severity-parse",
                        ("severity", args[4]),
                        ("help", Help)));
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
                shell.WriteError(Loc.GetString("cmd-chat-ban-arg-count"));
                shell.WriteLine(Help);

                return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            shell.WriteError(Loc.GetString("cmd-chat-ban-name-parse"));
            return;
        }

        var targetUid = located.UserId;
        var targetHWid = located.LastHWId;

        var chatBanInfo = new CreateChatsBanInfo(reason);
        if (minutes > 0)
            chatBanInfo.WithMinutes(minutes);
        chatBanInfo.AddUser(targetUid, located.Username);
        chatBanInfo.WithBanningAdmin(shell.Player?.UserId);
        chatBanInfo.WithBanningAdminName(shell.Player?.Name);
        chatBanInfo.AddHWId(targetHWid);
        chatBanInfo.WithSeverity(severity);
        chatBanInfo.WithPostBanInfo(postBanInfo);

        chatBanInfo.AddChat(bannableChat);

        _ban.CreateChatsBan(chatBanInfo);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var durOpts = new CompletionOption[]
        {
            new("0", Loc.GetString("cmd-chat-ban-hint-duration-1")),
            new("1440", Loc.GetString("cmd-chat-ban-hint-duration-2")),
            new("4320", Loc.GetString("cmd-chat-ban-hint-duration-3")),
            new("10080", Loc.GetString("cmd-chat-ban-hint-duration-4")),
            new("20160", Loc.GetString("cmd-chat-ban-hint-duration-5")),
            new("43800", Loc.GetString("cmd-chat-ban-hint-duration-6")),
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
            new("false", Loc.GetString("cmd-ban-hint-post-ban-false")),
        };

        var bannableChats = Enum.GetValues<BannableChats>()
            .Skip(1)
            .Select(c => c.ToString());

        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-chat-ban-hint-1")),
            2 => CompletionResult.FromHintOptions(bannableChats, Loc.GetString("cmd-chat-ban-hint-2")),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-chat-ban-hint-3")),
            4 => CompletionResult.FromHintOptions(durOpts, Loc.GetString("cmd-chat-ban-hint-4")),
            5 => CompletionResult.FromHintOptions(severities, Loc.GetString("cmd-chat-ban-hint-5")),
            6 => CompletionResult.FromHintOptions(postInfo, Loc.GetString("cmd-ban-hint-post-ban")),
            _ => CompletionResult.Empty,
        };
    }
}
