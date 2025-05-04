using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Mind;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AGhostCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    // SS220 additional command log
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override string Command => "aghost";
    public override string Help => "aghost";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var names = _playerManager.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return CompletionResult.FromHintOptions(names, LocalizationManager.GetString("shell-argument-username-optional-hint"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-wrong-arguments-number"));
            return;
        }

        var player = shell.Player;
        var self = player != null;
        if (player == null)
        {
            // If you are not a player, you require a player argument.
            if (args.Length == 0)
            {
                shell.WriteError(LocalizationManager.GetString("shell-need-exactly-one-argument"));
                return;
            }

            var didFind = _playerManager.TryGetSessionByUsername(args[0], out player);
            if (!didFind)
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        // If you are a player and a username is provided, a lookup is done to find the target player.
        if (args.Length == 1)
        {
            var didFind = _playerManager.TryGetSessionByUsername(args[0], out player);
            if (!didFind)
            {
                shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
                return;
            }
        }

        var mindSystem = _entities.System<SharedMindSystem>();
        var metaDataSystem = _entities.System<MetaDataSystem>();
        var ghostSystem = _entities.System<SharedGhostSystem>();
        var transformSystem = _entities.System<TransformSystem>();
        var gameTicker = _entities.System<GameTicker>();

        if (!mindSystem.TryGetMind(player, out var mindId, out var mind))
        {
            shell.WriteError(self
                ? LocalizationManager.GetString("aghost-no-mind-self")
                : LocalizationManager.GetString("aghost-no-mind-other"));
            return;
        }

        //SS220-lobby-ghost-bug begin
        if (player != null && (!gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) || status is not PlayerGameStatus.JoinedGame))
        {
            shell.WriteLine("You can't ghost right now. You're not in game!");
            return;
        }
        //SS220-lobby-ghost-bug end

        //SS220 admin action log
        LogAdminAction(shell, args);

        if (mind.VisitingEntity != default && _entities.TryGetComponent<GhostComponent>(mind.VisitingEntity, out var oldGhostComponent))
        {
            mindSystem.UnVisit(mindId, mind);
            // If already an admin ghost, then return to body.
            if (oldGhostComponent.CanGhostInteract)
                return;
        }

        var canReturn = mind.CurrentEntity != null
                        && !_entities.HasComponent<GhostComponent>(mind.CurrentEntity);
        var coordinates = player!.AttachedEntity != null
            ? _entities.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
            : gameTicker.GetObserverSpawnPoint();
        var ghost = SpawnGhost(coordinates, player, canReturn); // SS220 aghost-after-ghost
        transformSystem.AttachToGridOrMap(ghost, _entities.GetComponent<TransformComponent>(ghost));

        if (canReturn)
        {
            // TODO: Remove duplication between all this and "GamePreset.OnGhostAttempt()"...
            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
                metaDataSystem.SetEntityName(ghost, mind.CharacterName);
            else if (!string.IsNullOrWhiteSpace(player.Name))
                metaDataSystem.SetEntityName(ghost, player.Name);

            mindSystem.Visit(mindId, ghost, mind);
        }
        else
        {
            metaDataSystem.SetEntityName(ghost, player.Name);
            mindSystem.TransferTo(mindId, ghost, mind: mind);
        }

        var comp = _entities.GetComponent<GhostComponent>(ghost);
        ghostSystem.SetCanReturnToBody(comp, canReturn);

        //SS220-ghost-hats begin
        var actions = _entities.System<SharedActionsSystem>();
        actions.AddAction(ghost, ref comp.ToggleAGhostBodyVisualsActionEntity, comp.ToggleAGhostBodyVisualsAction);
        //SS220-ghost-hats end
    }

    /**
    * Choose ghost prototype based on current player's state:
    * - if player can return back to body -> aghost
    * - else if player is aghost -> ghost
    * - else -> aghost
    */
    private EntityUid SpawnGhost(EntityCoordinates coordinates, ICommonSession player, bool canReturn)
    {
        if (canReturn)
        {
            return _entities.SpawnEntity(GameTicker.AdminObserverPrototypeName, coordinates);
        }

        //check if current player is aghost
        var playerAttachedEntity = player.AttachedEntity;
        if (playerAttachedEntity is { Valid: true } playerEntity &&
            _entities.GetComponent<MetaDataComponent>(playerEntity).EntityPrototype?.ID == GameTicker.AdminObserverPrototypeName)
        {
            EmptyHands(playerAttachedEntity);
            return _entities.SpawnEntity(GameTicker.ObserverPrototypeName, coordinates);
        }

        return _entities.SpawnEntity(GameTicker.AdminObserverPrototypeName, coordinates);
    }

    private void EmptyHands(EntityUid? playerAttachedEntity)
    {
        if (playerAttachedEntity == null)
            return;
        var handsSystem = _entities.System<HandsSystem>();
        var handsComponent = _entities.GetComponent<HandsComponent>(playerAttachedEntity.Value);
        handsSystem.TryDrop(playerAttachedEntity.Value, checkActionBlocker: false, doDropInteraction: false,
            handsComp: handsComponent);
    }

    //SS220 admin action log start
    private void LogAdminAction(IConsoleShell shell, string[] args)
    {
        if (shell.Player is { } player)
        {
            if (args.Length == 0)
            {
                _adminLogger.Add(LogType.AdminCommand, $"{player} aghost self");
            }
            else
            {
                _adminLogger.Add(LogType.AdminCommand, $"{player} aghost {args[0]}");
            }
        }
        else
        {
            if (args.Length == 0)
            {
                _adminLogger.Add(LogType.AdminCommand, $"Unknown aghost self");
            }
            else
            {
                _adminLogger.Add(LogType.AdminCommand, $"Unknown aghost {args[0]}");
            }
        }

    }
    //SS220 admin action log end
}
