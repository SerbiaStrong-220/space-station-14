using Content.Server.Administration.Managers;
using Content.Shared.Mind;
using Content.Shared.SS220.Administration.Events;
using Content.Shared.SS220.Objectives;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.SS220.Administration.Systems;

public sealed class CustomObjectivesAdminSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private readonly Dictionary<NetUserId, CustomObjectivesPlayerInfo> _players = new();
    private readonly Dictionary<EntityUid, EntityUid> _objectiveOwners = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CustomObjectiveComponent, ComponentRemove>(OnCustomObjectiveRemoved);
        SubscribeLocalEvent<MindComponent, MindObjectivesChangedEvent>(OnMindObjectivesChanged);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        ScanExistingObjectives();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void ScanExistingObjectives()
    {
        var minds = EntityQueryEnumerator<MindComponent>();
        while (minds.MoveNext(out var mindUid, out var mind))
        {
            foreach (var objective in mind.Objectives)
            {
                if (HasComp<CustomObjectiveComponent>(objective))
                    _objectiveOwners[objective] = mindUid;
            }

            UpdatePlayer((mindUid, mind), false);
        }
    }

    private void OnCustomObjectiveRemoved(Entity<CustomObjectiveComponent> objective, ref ComponentRemove args)
    {
        if (!_objectiveOwners.Remove(objective.Owner, out var mindUid))
            return;

        if (TryComp(mindUid, out MindComponent? mind))
            UpdatePlayer((mindUid, mind));
    }

    private void OnMindObjectivesChanged(Entity<MindComponent> mind, ref MindObjectivesChangedEvent args)
    {
        if (args.Added)
        {
            if (!HasComp<CustomObjectiveComponent>(args.Objective))
                return;

            _objectiveOwners[args.Objective] = mind.Owner;
        }
        else if (!_objectiveOwners.Remove(args.Objective))
        {
            return;
        }

        UpdatePlayer(mind);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (!_mind.TryGetMind(args.Session, out var mindUid, out var mind))
        {
            if (_players.Remove(args.Session.UserId))
                SendCustomObjectivesList();

            return;
        }

        UpdatePlayer((mindUid, mind));
    }

    private void UpdatePlayer(Entity<MindComponent> mind, bool sendUpdate = true)
    {
        if (mind.Comp.UserId is not { } userId)
            return;

        var customObjectiveCount = 0;
        foreach (var objective in mind.Comp.Objectives)
        {
            if (HasComp<CustomObjectiveComponent>(objective))
                customObjectiveCount++;
        }

        if (customObjectiveCount == 0)
        {
            if (_players.Remove(userId) && sendUpdate)
                SendCustomObjectivesList();

            return;
        }

        if (!_playerManager.TryGetSessionById(userId, out var session))
        {
            if (_players.Remove(userId) && sendUpdate)
                SendCustomObjectivesList();

            return;
        }

        var characterName = session.AttachedEntity is { } entity ? Name(entity) : string.Empty;

        var playerInfo = new CustomObjectivesPlayerInfo(
            session.Name,
            characterName,
            GetNetEntity(session.AttachedEntity),
            userId,
            customObjectiveCount);

        var changed = !_players.TryGetValue(userId, out var oldPlayerInfo) || oldPlayerInfo != playerInfo;
        _players[userId] = playerInfo;

        if (sendUpdate && changed)
            SendCustomObjectivesList();
    }

    public void SendCustomObjectivesList(ICommonSession? admin = null)
    {
        var playerInfos = new List<CustomObjectivesPlayerInfo>(_players.Values);
        var ev = new CustomObjectivesPlayersEvent(playerInfos);

        if (admin is not null)
        {
            RaiseNetworkEvent(ev, admin.Channel);
            return;
        }

        foreach (var activeAdmin in _adminManager.ActiveAdmins)
        {
            RaiseNetworkEvent(ev, activeAdmin.Channel);
        }
    }

}
