// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Shuttles.Components;
using Content.Shared.SS220.CCVars;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.MalfAI;

public sealed partial class MalfAiRuleSystem : GameRuleSystem<MalfAiRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly ProtoId<AntagPrototype> MalfAntag = "MalfunctioningAI";
    private static readonly EntProtoId MalfMindRole = "MindRoleMalfAi";
    private static readonly EntProtoId HijackObjective = "MalfAiHijackShuttleObjective";
    private static readonly ProtoId<JobPrototype> StationAiJob = "StationAi";

    private static readonly LocId RoleGreeting = "malfai-role-greeting";

    private readonly HashSet<EntityUid> _activeRules = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _activeRules.Clear();
    }

    protected override void Started(EntityUid uid,
        MalfAiRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _activeRules.Add(uid);
        TrySelect(component);
    }

    protected override void Ended(EntityUid uid,
        MalfAiRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        _activeRules.Remove(uid);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent args)
    {
        if (_activeRules.Count == 0 || args.JobId != StationAiJob.Id)
            return;

        foreach (var rule in _activeRules)
        {
            if (TryComp<MalfAiRuleComponent>(rule, out var comp))
                TrySelect(comp);
        }
    }

    private void TrySelect(MalfAiRuleComponent component)
    {
        if (component.SelectionDone)
            return;

        if (!_cfg.GetCVar(CCVars220.MalfAiEnabled))
            return;

        if (HasAnyMalfAi())
            return;

        var candidates = CollectCandidates(out var aliveAis);
        if (candidates.Count == 0)
        {
            if (aliveAis > 0)
                Log.Info($"Malf AI rule has {aliveAis} alive station AI(s) but no valid candidates " +
                         "(preference off / no owned entity). Selection stays pending.");
            return;
        }

        if (AssignMalf(_random.Pick(candidates)))
            component.SelectionDone = true;
    }

    private bool HasAnyMalfAi()
    {
        var query = EntityQueryEnumerator<MalfAiRoleComponent>();
        return query.MoveNext(out _, out _);
    }

    private List<Entity<MindComponent>> CollectCandidates(out int aliveAis)
    {
        var alive = new HashSet<Entity<MindComponent>>();
        _stationAi.AddAliveAis(alive);
        aliveAis = alive.Count;

        var result = new List<Entity<MindComponent>>();
        var preference = new List<ProtoId<AntagPrototype>> { MalfAntag };
        foreach (var (mindId, mindComp) in alive)
        {
            if (_roles.MindHasRole<MalfAiRoleComponent>(mindId)
                || _roles.MindIsExclusiveAntagonist(mindId))
                continue;

            if (!_player.TryGetSessionById(mindComp.UserId, out var session)
                || !_antag.ValidAntagPreference(session, preference))
                continue;

            if (mindComp.OwnedEntity is not { } body
                || !_stationAi.TryGetCore(body, out _))
                continue;

            result.Add((mindId, mindComp));
        }

        return result;
    }

    public bool AssignMalf(Entity<MindComponent> mindEnt)
    {
        var (mindId, mindComp) = mindEnt;

        if (_roles.MindHasRole<MalfAiRoleComponent>(mindId))
            return false;

        _roles.MindAddRole(mindId, MalfMindRole, mindComp);
        if (!_mind.TryAddObjective(mindId, mindComp, HijackObjective))
        {
            _roles.MindRemoveRole<MalfAiRoleComponent>(mindId);
            return false;
        }

        if (mindComp.OwnedEntity is { } body)
            _antag.SendBriefing(body, Loc.GetString(RoleGreeting), null, null);

        Log.Info($"Malf AI assigned to mind {mindId}.");
        return true;
    }
}
