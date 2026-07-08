using System.Linq;
using Content.Server.Roles;
using Content.Server.Administration.Managers;
using Content.Server.Roles.Jobs;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.Administration.Events;
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

    private EntityQuery<ObjectiveComponent> _objectiveQuery;

    private readonly Dictionary<NetUserId, CustomObjectivesPlayerInfo> _customObjectivesPlayers = new();
    private readonly Dictionary<EntityUid, EntityUid> _customObjectiveOwners = new();

    public override void Initialize()
    {
        base.Initialize();

        _objectiveQuery = GetEntityQuery<ObjectiveComponent>();

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

        SendCustomObjectivesList();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnObjectiveRemove(Entity<ObjectiveComponent> objective, ref ComponentRemove _args)
    {
        if (!objective.Comp.Completed.HasValue)
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

    private void OnMindObjectivesChanged(Entity<MindComponent> mind, ref MindObjectivesChangedEvent _args)
    {
        UpdateCustomObjectivesPlayer(mind);
    }

    private void UpdateCustomObjectivesPlayer(Entity<MindComponent> mind, bool sendUpdate = true)
    {
        if (mind.Comp.UserId == null)
            return;

        if (_roles.MindIsAntagonist(mind))
        {
            RemoveOwnedCustomObjectives(mind);
            _customObjectivesPlayers.Remove(mind.Comp.UserId.Value);
            if (sendUpdate)
                SendCustomObjectivesList();
            return;
        }

        var customObjectives = new List<EntityUid>();
        foreach (var objective in mind.Comp.Objectives)
        {
            if (_objectiveQuery.TryGetComponent(objective, out var objComp) && objComp.Completed.HasValue)
                customObjectives.Add(objective);
        }

        SyncOwnedCustomObjectives(mind, customObjectives);
        var customObjectiveCount = customObjectives.Count;

        if (customObjectiveCount == 0)
        {
            _customObjectivesPlayers.Remove(mind.Comp.UserId.Value);
            if (sendUpdate)
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

        _customObjectivesPlayers[mind.Comp.UserId.Value] = playerInfo;
        if (sendUpdate)
            SendCustomObjectivesList();
    }

    public void SendCustomObjectivesList(ICommonSession? admin = null)
    {
        var ev = new CustomObjectivesPlayersEvent
        {
            Players = _customObjectivesPlayers.Values.ToList()
        };

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

    private void SyncOwnedCustomObjectives(Entity<MindComponent> mind, List<EntityUid> currentObjectives)
    {
        var staleObjectives = _customObjectiveOwners
            .Where(pair => pair.Value == mind.Owner && !currentObjectives.Contains(pair.Key))
            .Select(pair => pair.Key)
            .ToArray();

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
        var objectives = _customObjectiveOwners
            .Where(pair => pair.Value == mind.Owner)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var objective in objectives)
        {
            _customObjectiveOwners.Remove(objective);
        }
    }
}
