#if DEBUG

using Content.Server.Administration;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Robust.Server.SS220.Player;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.SS220.Player;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed partial class DebugSpawnPlayerCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private int _nextPlayerId = 1;

    public string Command => "debug_spawn_player";
    public string Description => "Creates a server-only debug player session and mob.";
    public string Help => $"Usage: {Command} [count|name]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var mindSys = _entityManager.System<SharedMindSystem>();
        var spawning = _entityManager.System<StationSpawningSystem>();
        var stationSys = _entityManager.System<StationSystem>();

        if (args.Length > 1)
        {
            shell.WriteError(Help);
            return;
        }

        var count = 1;
        string? requestedName = null;
        if (args.Length == 1)
        {
            if (int.TryParse(args[0], out var parsedCount))
            {
                if (parsedCount < 1)
                {
                    shell.WriteError("Count must be greater than zero.");
                    return;
                }

                count = parsedCount;
            }
            else
            {
                requestedName = args[0];
            }
        }

        EntityUid? station = null;
        foreach (var candidate in stationSys.GetStations())
        {
            if (!_entityManager.HasComponent<StationSpawningComponent>(candidate))
                continue;

            station = candidate;
            break;
        }

        if (station is not { } stationUid)
        {
            shell.WriteError("No station with a valid spawning setup is available.");
            return;
        }

        var debugPlayerManager = (IDebugPlayerManager) _playerManager;
        for (var i = 0; i < count; i++)
        {
            var name = requestedName ?? NextName();
            if (_playerManager.TryGetSessionByUsername(name, out _))
            {
                shell.WriteError($"A session named '{name}' already exists.");
                return;
            }

            if (!TrySpawnPlayer(shell, name, stationUid, mindSys, spawning, stationSys, debugPlayerManager))
                return;
        }
    }

    private bool TrySpawnPlayer(
        IConsoleShell shell,
        string name,
        EntityUid stationUid,
        SharedMindSystem mindSys,
        StationSpawningSystem spawning,
        StationSystem stationSys,
        IDebugPlayerManager debugPlayerManager)
    {
        var session = debugPlayerManager.AddDebugSession(name);
        EntityUid? entity = null;
        EntityUid? mind = null;

        try
        {
            var profile = HumanoidCharacterProfile.Random();
            var spawnedEntity = spawning.SpawnPlayerCharacterOnStation(stationUid, null, profile, false);
            if (spawnedEntity is not { } spawned)
                throw new InvalidOperationException("No valid station spawn point is available.");

            if (stationSys.GetOwningStation(spawned) != stationUid)
                throw new InvalidOperationException("The selected spawn point does not belong to the station.");

            entity = spawned;

            var createdMind = mindSys.CreateMind(session.UserId, profile.Name);
            mind = createdMind.Owner;
            mindSys.TransferTo(createdMind.Owner,
                entity.Value,
                ghostCheckOverride: true,
                createGhost: false,
                mind: createdMind.Comp);

            _playerManager.JoinGame(session);

            shell.WriteLine($"Created debug player '{session.Name}' ({session.UserId}) as {entity.Value} on station {stationUid}.");
            return true;
        }
        catch (Exception e)
        {
            if (mind is { } mindUid)
                mindSys.WipeMind(mindUid);

            if (entity is { } entityUid && _entityManager.EntityExists(entityUid))
                _entityManager.DeleteEntity(entityUid);

            debugPlayerManager.RemoveDebugSession(session);
            shell.WriteError($"Failed to create debug player '{name}': {e.Message}");
            return false;
        }
    }

    private string NextName()
    {
        string name;
        do
        {
            name = $"debug_player_{_nextPlayerId++}";
        } while (_playerManager.TryGetSessionByUsername(name, out _));

        return name;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed partial class DebugDespawnPlayerCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public string Command => "debug_despawn_player";
    public string Description => "Removes a server-only debug player session and mob.";
    public string Help => $"Usage: {Command} <name> | {Command} true";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var mindSys = _entityManager.System<SharedMindSystem>();

        if (args.Length == 1 && bool.TryParse(args[0], out var removeAll))
        {
            if (!removeAll)
            {
                shell.WriteError(Help);
                return;
            }

            var removed = 0;
            foreach (var debugSession in _playerManager.Sessions)
            {
                if (debugSession.Channel is not DebugNetChannel)
                    continue;

                if (TryDespawnPlayer(debugSession, mindSys))
                    removed++;
            }

            shell.WriteLine($"Removed {removed} debug players.");
            return;
        }

        if (args.Length != 1 || !_playerManager.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError(Help);
            return;
        }

        if (!TryDespawnPlayer(session, mindSys))
        {
            shell.WriteError($"'{session.Name}' is not debug player.");
            return;
        }

        shell.WriteLine($"Removed debug player '{session.Name}'.");
    }

    private bool TryDespawnPlayer(ICommonSession session, SharedMindSystem mindSys)
    {
        var entity = session.AttachedEntity;
        var mind = session.GetMind();
        var debugPlayerManager = (IDebugPlayerManager) _playerManager;
        if (session.Channel is not DebugNetChannel || !debugPlayerManager.RemoveDebugSession(session))
            return false;

        if (mind is { } mindUid)
            mindSys.WipeMind(mindUid);

        if (entity is { } entityUid && _entityManager.EntityExists(entityUid))
            _entityManager.DeleteEntity(entityUid);

        return true;
    }
}

#endif
