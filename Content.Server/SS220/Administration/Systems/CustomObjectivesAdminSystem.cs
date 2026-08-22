using Content.Server.Roles;
using Content.Server.Administration.Managers;
using Content.Server.Roles.Jobs;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.Administration.Events;
using Robust.Shared.Network;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.SS220.Administration.Systems;

public sealed partial class CustomObjectivesAdminSystem : EntitySystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private JobSystem _jobs = default!;
    [Dependency] private RoleSystem _roles = default!;

    private readonly Dictionary<NetUserId, CustomObjectivesPlayerInfo> _customObjectivesPlayers = new();
    private readonly Dictionary<EntityUid, EntityUid> _customObjectiveOwners = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObjectiveComponent, ComponentRemove>(OnObjectiveRemove);
        SubscribeLocalEvent<MindComponent, MindObjectivesChangedEvent>(OnMindObjectivesChanged);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        ScanExistingObjectives();
    }

    private void ScanExistingObjectives()
    {
        var mindQuery = AllEntityQuery<MindComponent>();
        while (mindQuery.MoveNext(out var mindId, out var mindComp))
        {
            UpdateCustomObjectivesPlayer((mindId, mindComp), false);
        }

    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnObjectiveRemove(Entity<ObjectiveComponent> objective, ref ComponentRemove _args)
    {
        if (!IsCustomObjective(objective.Comp))
            return;

        if (!_customObjectiveOwners.Remove(objective, out var mindId))
            return;

        if (TryComp(mindId, out MindComponent? mindComp))
            UpdateCustomObjectivesPlayer((mindId, mindComp));
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (!_mind.TryGetMind(e.Session, out var mindId, out var mindComp))
        {
            if (_customObjectivesPlayers.Remove(e.Session.UserId))
                SendCustomObjectivesList();

            return;
        }

        UpdateCustomObjectivesPlayer((mindId, mindComp));
    }

    private void OnMindObjectivesChanged(Entity<MindComponent> mind, ref MindObjectivesChangedEvent args)
    {
        if (args.Added)
        {
            if (!TryComp(args.Objective, out ObjectiveComponent? objectiveComp) || !IsCustomObjective(objectiveComp))
                return;
        }
        else if (!_customObjectiveOwners.ContainsKey(args.Objective))
        {
            return;
        }

        UpdateCustomObjectivesPlayer(mind);
    }

    private void UpdateCustomObjectivesPlayer(Entity<MindComponent> mind, bool sendUpdate = true)
    {
        if (mind.Comp.UserId == null)
            return;

        if (_roles.MindIsAntagonist(mind))
        {
            RemoveOwnedCustomObjectives(mind);
            var removed = _customObjectivesPlayers.Remove(mind.Comp.UserId.Value);
            if (removed && sendUpdate)
                SendCustomObjectivesList();
            return;
        }

        var customObjectives = new HashSet<EntityUid>();
        foreach (var objective in mind.Comp.Objectives)
        {
            if (!TryComp(objective, out ObjectiveComponent? objectiveComp) || !IsCustomObjective(objectiveComp))
                continue;

            customObjectives.Add(objective);
        }

        SyncOwnedCustomObjectives(mind, customObjectives);
        var customObjectiveCount = customObjectives.Count;

        if (customObjectiveCount == 0)
        {
            var removed = _customObjectivesPlayers.Remove(mind.Comp.UserId.Value);
            if (removed && sendUpdate)
                SendCustomObjectivesList();
            return;
        }

        if (!_playerManager.TryGetSessionById(mind.Comp.UserId.Value, out var session))
        {
            if (_customObjectivesPlayers.Remove(mind.Comp.UserId.Value) && sendUpdate)
                SendCustomObjectivesList();

            return;
        }

        var entityName = string.Empty;
        var identityName = string.Empty;
        var startingJob = string.Empty;

        if (session.AttachedEntity != null)
        {
            entityName = Name(session.AttachedEntity.Value);
            identityName = Identity.Name(session.AttachedEntity.Value, EntityManager);
        }

        if (_jobs.MindTryGetJobName(mind, out var jobName))
        {
            startingJob = jobName;
        }

        var playerInfo = new CustomObjectivesPlayerInfo(
            session.Name,
            entityName,
            identityName,
            startingJob,
            GetNetEntity(session.AttachedEntity),
            session.UserId,
            session.Status == SessionStatus.InGame,
            session.Status is not SessionStatus.Disconnected and not SessionStatus.Zombie,
            customObjectiveCount
        );

        var changed =
            !_customObjectivesPlayers.TryGetValue(mind.Comp.UserId.Value, out var previousInfo) ||
            previousInfo != playerInfo;

        _customObjectivesPlayers[mind.Comp.UserId.Value] = playerInfo;

        if (sendUpdate && changed)
            SendCustomObjectivesList();
    }

    public void SendCustomObjectivesList(ICommonSession? admin = null)
    {
        var players = new List<CustomObjectivesPlayerInfo>(_customObjectivesPlayers.Count);
        foreach (var playerInfo in _customObjectivesPlayers.Values)
        {
            players.Add(playerInfo);
        }

        var ev = new CustomObjectivesPlayersEvent(players);

        if (admin != null)
        {
            RaiseNetworkEvent(ev, admin.Channel);
            return;
        }

        foreach (var activeAdmin in _adminManager.ActiveAdmins)
        {
            RaiseNetworkEvent(ev, activeAdmin.Channel);
        }
    }

    private void SyncOwnedCustomObjectives(Entity<MindComponent> mind, HashSet<EntityUid> currentObjectives)
    {
        var staleObjectives = new List<EntityUid>();

        foreach (var (objective, owner) in _customObjectiveOwners)
        {
            if (owner != mind.Owner || currentObjectives.Contains(objective))
                continue;

            staleObjectives.Add(objective);
        }

        foreach (var staleObjective in staleObjectives)
        {
            _customObjectiveOwners.Remove(staleObjective);
        }

        foreach (var objective in currentObjectives)
        {
            _customObjectiveOwners[objective] = mind.Owner;
        }
    }

    private void RemoveOwnedCustomObjectives(Entity<MindComponent> mind)
    {
        var objectives = new List<EntityUid>();

        foreach (var (objective, owner) in _customObjectiveOwners)
        {
            if (owner != mind.Owner)
                continue;

            objectives.Add(objective);
        }

        foreach (var objective in objectives)
        {
            _customObjectiveOwners.Remove(objective);
        }
    }

    private static bool IsCustomObjective(ObjectiveComponent objective)
    {
        return objective.Completed is not null;
    }
}
