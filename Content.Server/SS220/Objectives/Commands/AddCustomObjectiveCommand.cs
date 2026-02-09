using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Objectives.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddCustomObjectiveCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override string Command => "addcustomobjective";
    public override string Help => Loc.GetString("add-custom-objective-command-help");
    public override string Description => Loc.GetString("add-custom-objective-command-description");

    private string BaseLocIssuer => LocalizationManager.GetString("free-objectives-name");
    private static readonly SpriteSpecifier BaseIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Decals/stencil.rsi/stencil_Plus.png"));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3 || args.Length > 5)
        {
            shell.WriteError(Help);
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var data))
            return;

        var name = args[1];
        var desc = args[2];

        var icon = BaseIcon;

        if (args.Length >= 4 && !string.IsNullOrEmpty(args[3]))
        {
            if (ResPath.IsValidPath(args[3]))
                icon = new SpriteSpecifier.Texture(new ResPath(args[3]));
        }

        var issuer = BaseLocIssuer;

        if (args.Length >= 5 && !string.IsNullOrEmpty(args[4]))
            issuer = args[4];

        if (!_mind.TryGetMind(data, out var mindId, out var mind))
            return;

        if (!_mind.TryAddObjectiveWithMetadata(mindId, mind, name, desc, icon, issuer))
            return;

        var msg = Loc.GetString("ui-add-objective-chat-manager-announce");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, data.Channel, colorOverride: Color.Red);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("add-custom-objective-command-first-param-text")),
            2 => CompletionResult.FromHint(Loc.GetString("add-custom-objective-command-second-param-text")),
            3 => CompletionResult.FromHint(Loc.GetString("add-custom-objective-command-third-param-text")),
            4 => CompletionResult.FromHint(Loc.GetString("add-custom-objective-command-fourth-param-text")),
            5 => CompletionResult.FromHint(Loc.GetString("add-custom-objective-command-fifth-param-text")),
            _ => CompletionResult.Empty,
        };
    }
}
