using System.Linq;
using Content.Server.Roles;
using Content.Server.Administration.Managers;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Shared.Administration;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.Administration.Events;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.Administration.Systems;

public sealed class CustomObjectivesAdminSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly RoleSystem _roles = default!;

    private EntityQuery<ObjectiveComponent> _objectiveQuery;

    private readonly Dictionary<NetUserId, CustomObjectivesPlayerInfo> _customObjectivesPlayers = new();
    private readonly Dictionary<EntityUid, EntityUid> _customObjectiveOwners = new();

    public override void Initialize()
    {
        base.Initialize();

        _objectiveQuery = GetEntityQuery<ObjectiveComponent>();

        SubscribeLocalEvent<ObjectiveComponent, ComponentInit>(OnObjectiveInit);
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
            UpdateCustomObjectivesPlayer(mindId, mindComp);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnObjectiveInit(EntityUid uid, ObjectiveComponent comp, ComponentInit args)
    {
        if (!comp.Completed.HasValue)
            return;

        var mindQuery = AllEntityQuery<MindComponent>();
        while (mindQuery.MoveNext(out var mindId, out var mindComp))
        {
            if (mindComp.Objectives.Contains(uid))
            {
                UpdateCustomObjectivesPlayer(mindId, mindComp);
                break;
            }
        }
    }

    private void OnObjectiveRemove(EntityUid uid, ObjectiveComponent comp, ComponentRemove args)
    {
        if (!comp.Completed.HasValue)
            return;

        if (!_customObjectiveOwners.Remove(uid, out var mindId))
            return;

        if (TryComp(mindId, out MindComponent? mindComp))
            UpdateCustomObjectivesPlayer(mindId, mindComp);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (!_mind.TryGetMind(e.Session, out var mindId, out var mindComp))
        {
            if (_customObjectivesPlayers.Remove(e.Session.UserId))
                SendCustomObjectivesList();

            return;
        }

        UpdateCustomObjectivesPlayer(mindId, mindComp);
    }

    private void OnMindObjectivesChanged(EntityUid uid, MindComponent component, ref MindObjectivesChangedEvent args)
    {
        UpdateCustomObjectivesPlayer(uid, component);
    }

    private void UpdateCustomObjectivesPlayer(EntityUid mindId, MindComponent mindComp)
    {
        if (mindComp.UserId == null)
            return;

        if (_roles.MindIsAntagonist(mindId))
        {
            RemoveOwnedCustomObjectives(mindId);
            _customObjectivesPlayers.Remove(mindComp.UserId.Value);
            SendCustomObjectivesList();
            return;
        }

        var customObjectives = new List<EntityUid>();
        foreach (var objective in mindComp.Objectives)
        {
            if (_objectiveQuery.TryGetComponent(objective, out var objComp) && objComp.Completed.HasValue)
                customObjectives.Add(objective);
        }

        SyncOwnedCustomObjectives(mindId, customObjectives);
        var customObjectiveCount = customObjectives.Count;

        if (customObjectiveCount == 0)
        {
            _customObjectivesPlayers.Remove(mindComp.UserId.Value);
            SendCustomObjectivesList();
            return;
        }

        if (!_playerManager.TryGetSessionById(mindComp.UserId.Value, out var session))
        {
            if (_customObjectivesPlayers.Remove(mindComp.UserId.Value))
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

        if (_jobs.MindTryGetJobName(mindId, out var jobName))
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

        _customObjectivesPlayers[mindComp.UserId.Value] = playerInfo;
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

    private void SyncOwnedCustomObjectives(EntityUid mindId, List<EntityUid> currentObjectives)
    {
        var staleObjectives = _customObjectiveOwners
            .Where(pair => pair.Value == mindId && !currentObjectives.Contains(pair.Key))
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var staleObjective in staleObjectives)
        {
            _customObjectiveOwners.Remove(staleObjective);
        }

        foreach (var objective in currentObjectives)
        {
            _customObjectiveOwners[objective] = mindId;
        }
    }

    private void RemoveOwnedCustomObjectives(EntityUid mindId)
    {
        var objectives = _customObjectiveOwners
            .Where(pair => pair.Value == mindId)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var objective in objectives)
        {
            _customObjectiveOwners.Remove(objective);
        }
    }
}
