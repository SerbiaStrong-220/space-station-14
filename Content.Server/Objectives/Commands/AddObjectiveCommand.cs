using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddObjectiveCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    // ss220 add custom antag goals start
    [Dependency] private readonly TargetObjectiveSystem _targetObjective = default!;
    [Dependency] private readonly StealConditionSystem _steal = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    // ss220 add custom antag goals end

    public override string Command => "addobjective";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // ss220 add custom antag goals start
        // if (args.Length != 2)
        // {
        //     shell.WriteError(Loc.GetString(Loc.GetString("cmd-addobjective-invalid-args")));
        //     return;
        // }
        // ss220 add custom antag goals end

        // ss220 add custom antag goals start
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString(Loc.GetString("cmd-addobjective-invalid-args")));
            return;
        }
        // ss220 add custom antag goals end

        if (!_players.TryGetSessionByUsername(args[0], out var data))
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-player-not-found"));
            return;
        }

        if (!_mind.TryGetMind(data, out var mindId, out var mind))
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-mind-not-found"));
            return;
        }

        if (!_prototypes.TryIndex<EntityPrototype>(args[1], out var proto) ||
            !proto.HasComponent<ObjectiveComponent>())
        {
            shell.WriteError(Loc.GetString("cmd-addobjective-objective-not-found", ("obj", args[1])));
            return;
        }

        // ss220 add custom antag goals start
        EntityUid? targetEnt = null;
        EntityPrototype? targetEntProto = null;
        var force = false;

        if (args.Length >= 3)
        {
            if (EntityUid.TryParse(args[2], out var entUid))
                targetEnt = entUid;

            if (_prototypes.TryIndex(args[2], out var targetProto))
                targetEntProto = targetProto;

            bool.TryParse(args[2], out force);
        }

        if (args.Length == 4)
        {
            force = bool.Parse(args[3]);
        }

        if (!_mind.TryAddObjective(mindId, mind, args[1], out var objective, force))
        {
            // can fail for other reasons so dont pretend to be right
            shell.WriteError(Loc.GetString("cmd-addobjective-adding-failed"));
            return;
        }

        if (EntityManager.TryGetComponent<TargetObjectiveComponent>(objective, out var targetObj))
        {
            if (targetEnt != null)
            {
                _targetObjective.SetTarget(objective.Value, targetEnt.Value, targetObj);
                _targetObjective.ResetEntityName(objective.Value, log: true);
            }
            else
            {
                _mind.TryRemoveObjective(mindId, mind, objective.Value);
                return;
            }
        }

        if (EntityManager.TryGetComponent<StealConditionComponent>(objective, out var stealCondition))
        {
            if (targetEnt != null)
                _steal.SetStealTarget(targetEnt.Value, (objective.Value, stealCondition));
            else if (targetEntProto != null)
                _steal.SetStealTarget(targetEntProto, (objective.Value, stealCondition));
            else
                return;
        }

        var msg = Loc.GetString("ui-add-objective-chat-manager-announce");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, data.Channel, colorOverride: Color.Red);
        // ss220 add custom antag goals end
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-addobjective-player-completion"));
        }

        if (args.Length != 2)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(
            _objectives.Objectives(),
            Loc.GetString(Loc.GetString("cmd-add-objective-obj-completion")));
    }
}
