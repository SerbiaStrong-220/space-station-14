// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Server.SS220.AntagRemoval.Notifications;
using Content.Server.SS220.AntagRemoval.Handlers;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.GameTicking;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.AntagRemoval;

/// <summary>Safely removes one supported antagonist role without removing the player's job.</summary>
public sealed partial class RemoveAntagSystem : EntitySystem
{
    private const string MiGoJobId = "MiGoJob";

    [Dependency] private EuiManager _euiManager = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private ISharedAdminLogManager _log = default!;
    [Dependency] private AntagSelectionSystem _selection = default!;
    [Dependency] private RoleSystem _roles = default!;
    [Dependency] private JobSystem _jobs = default!;
    [Dependency] private NpcFactionSystem _factions = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ExperienceAdditionSystem _experienceAddition = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        InitializeUi();
        EntityManager.EntityAdded += OnGrantedEntityAdded;
    }

    public override void Shutdown()
    {
        EntityManager.EntityAdded -= OnGrantedEntityAdded;
        base.Shutdown();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _selection.ResetRemovedRolesForNewRound();
    }

    private static readonly Dictionary<string, string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        ["traitor"] = RemoveAntagRoles.Traitor,
        ["thief"] = RemoveAntagRoles.Thief,
        ["cult"] = RemoveAntagRoles.Cult,
        ["cultist"] = RemoveAntagRoles.Cult,
        ["initialinfected"] = RemoveAntagRoles.InitialInfected,
        ["headrev"] = RemoveAntagRoles.HeadRevolutionary,
    };

    public string GetRemovableRoles(ICommonSession session)
    {
        if (!TryResolvePlayer(session, out var body, out var mindId, out var mind, out var error))
            return error;

        var roles = Supported
            .Where(p => p.Key != "cultist" && HasRolePrototype(mind, p.Value))
            .Select(p => p.Key)
            .ToArray();
        return Loc.GetString("ss220-removeantag-list-roles",
            ("player", session.Name),
            ("character", Name(body)),
            ("job", GetJob(mindId)?.ID ?? "—"),
            ("mind", mindId.ToString()),
            ("body", body.ToString()),
            ("roles", roles.Length == 0
                ? Loc.GetString("ss220-removeantag-list-empty")
                : string.Join(", ", roles)));
    }

    public bool TryRemove(ICommonSession? administrator, ICommonSession session, string kind, out string result,
        NetEntity? expectedBody = null, NetEntity? expectedMind = null)
    {
        if (kind is null || !Supported.TryGetValue(kind, out var roleId))
        {
            result = Loc.GetString("ss220-removeantag-error-unsupported");
            return false;
        }

        if (!TryResolvePlayer(session, out var body, out var mindId, out var mind, out result))
            return false;

        if ((expectedBody is { } bodyNet && GetNetEntity(body) != bodyNet) ||
            (expectedMind is { } mindNet && GetNetEntity(mindId) != mindNet))
        {
            result = Loc.GetString("ss220-removeantag-error-target-changed");
            return false;
        }

        if (!HasRolePrototype(mind, roleId))
        {
            result = Loc.GetString("ss220-removeantag-error-role-missing", ("role", kind));
            return false;
        }

        if (!TryComp<AntagRemovalGrantComponent>(body, out var provenance) ||
            !provenance.Grants.TryGetValue(roleId, out var grant) ||
            grant is null || !grant.VerifiedProvenance || grant.GrantedMindId != mindId)
        {
            result = Loc.GetString("ss220-removeantag-error-unavailable");
            return false;
        }

        var job = GetJob(mindId);
        if (job is null || job.ID == MiGoJobId || HasComp<MiGoComponent>(body) || HasComp<ZombieComponent>(body))
        {
            result = Loc.GetString("ss220-removeantag-error-invalid-target");
            return false;
        }

        var handler = GetHandler(roleId);
        if (handler is null)
        {
            result = Loc.GetString("ss220-removeantag-error-unavailable");
            return false;
        }

        if (!handler.CanRemove(body, mindId, out result))
            return false;

        if (!CanRevokeAntagExperience(body, roleId, out result))
            return false;

        if (roleId == RemoveAntagRoles.InitialInfected &&
            ((HasComp<InitialInfectedComponent>(body) && !grant.GrantedPatientZero) ||
             (HasComp<ZombifyOnDeathComponent>(body) && !grant.GrantedZombifyOnDeath) ||
             (HasComp<PendingZombieComponent>(body) && !grant.GrantedPendingZombie) ||
             (HasComp<IncurableZombieComponent>(body) && !grant.GrantedIncurableZombie)))
        {
            result = Loc.GetString("ss220-removeantag-error-unavailable");
            return false;
        }

        var lostPoints = GetRoleBonusPoints(body, roleId);

        if (!handler.RemovesMindRole && !_roles.MindRemoveRole(mindId, roleId))
        {
            result = Loc.GetString("ss220-removeantag-error-role-missing", ("role", kind));
            return false;
        }

        if (!handler.TryRemove(body, mindId, provenance, grant, out result))
            return false;

        if (TryComp<MindComponent>(mindId, out var currentMind))
        {
            foreach (var objective in grant.Objectives.ToArray())
                if (currentMind.Objectives.Contains(objective))
                    EntityManager.System<SharedMindSystem>().TryRemoveObjective(mindId, currentMind, objective);
        }

        RecalculateExperience(body, job, roleId, lostPoints);

        RevokeStartingGear(grant);

        ReconcileFactions(body, provenance, grant);

        var assignmentStillActive = provenance.Grants.Values.Any(other =>
            other != grant && other.GameRule == grant.GameRule &&
            grant.SelectionDefinitionIndex >= 0 &&
            other.SelectionDefinitionIndex == grant.SelectionDefinitionIndex &&
            HasRolePrototype(mind, other.RoleId));
        _selection.RevokeAssignment(session, mindId, roleId, grant.GameRule,
            assignmentStillActive ? -1 : grant.SelectionDefinitionIndex);
        provenance.Grants.Remove(roleId);

        if (roleId == RemoveAntagRoles.HeadRevolutionary)
            EntityManager.System<RevolutionaryRuleSystem>().RecheckAfterHeadRemoval(grant.GameRule);

        RaiseNetworkEvent(new MindRoleTypeChangedEvent(), session.Channel);

        var roleName = RoleName(roleId);
        var message = Loc.GetString("ss220-removeantag-hidden", ("role", roleName));
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, session.Channel);

        if (roleId != RemoveAntagRoles.Cult)
            _euiManager.OpenEui(new RoleRemovalNoticeEui(roleName), session);

        _log.Add(LogType.Mind, LogImpact.High,
            $"removeantag: actor={FormatAdministrator(administrator)}, target={session.Name} ({session.UserId}), character={Name(body)}, removed={roleId}, stationJob={job.ID}");

        result = Loc.GetString("ss220-removeantag-result-success",
            ("role", roleName), ("player", session.Name), ("job", job.ID));
        return true;
    }

    private static string FormatAdministrator(ICommonSession? administrator)
        => administrator is null
            ? "SERVER_CONSOLE"
            : $"{administrator.Name} ({administrator.UserId})";

    private bool TryResolvePlayer(ICommonSession session, out EntityUid body,
        out EntityUid mindId, out MindComponent mind, out string reason)
    {
        body = default;
        mindId = default;
        mind = default!;
        reason = string.Empty;

        if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie ||
            session.AttachedEntity is not { } entity || TerminatingOrDeleted(entity))
        {
            reason = Loc.GetString("ss220-removeantag-error-invalid-target");
            return false;
        }

        if (!EntityManager.System<SharedMindSystem>().TryGetMind(entity, out mindId, out var resolvedMind) ||
            resolvedMind is null ||
            resolvedMind.OwnedEntity != entity ||
            resolvedMind.UserId != session.UserId)
        {
            reason = Loc.GetString("ss220-removeantag-error-invalid-target");
            return false;
        }

        mind = resolvedMind;
        body = entity;
        return true;
    }

    // Register one handler for each supported MindRole.
    private IAntagRemovalHandler? GetHandler(string roleId) => roleId switch
    {
        RemoveAntagRoles.Traitor => EntityManager.System<TraitorRemovalSystem>(),
        RemoveAntagRoles.Thief => EntityManager.System<ThiefRemovalSystem>(),
        RemoveAntagRoles.Cult => EntityManager.System<CultRemovalSystem>(),
        RemoveAntagRoles.InitialInfected => EntityManager.System<InitialInfectedRemovalSystem>(),
        RemoveAntagRoles.HeadRevolutionary => EntityManager.System<HeadRevolutionaryRemovalSystem>(),
        _ => null
    };

    private JobPrototype? GetJob(EntityUid mindId)
        => _jobs.MindTryGetJob(mindId, out var job) ? job : null;

    private bool HasRolePrototype(MindComponent mind, string roleId)
    {
        foreach (var role in mind.MindRoleContainer.ContainedEntities)
            if (MetaData(role).EntityPrototype?.ID == roleId)
                return true;

        return false;
    }

    private void RevokeStartingGear(AntagGrantRecord grant)
    {
        foreach (var item in grant.Gear)
        {
            if (TerminatingOrDeleted(item))
                continue;

            if (TryComp<ContainerManagerComponent>(item, out var manager))
            {
                foreach (var container in _containers.GetAllContainers(item, manager))
                    foreach (var contents in container.ContainedEntities.ToArray())
                        _containers.Remove(contents, container, force: true);
            }

            QueueDel(item);
        }
    }

    private void ReconcileFactions(EntityUid body, AntagRemovalGrantComponent origin,
        AntagGrantRecord removed)
    {
        foreach (var faction in removed.FactionsAdded)
        {
            var stillGranted = origin.Grants.Values.Any(other => other != removed &&
                other.FactionsAdded.Contains(faction));
            if (!stillGranted && origin.FactionsBeforeFirstAntag?.Contains(faction) != true)
                _factions.RemoveFaction(body, faction.Id);
        }

        foreach (var faction in removed.FactionsRemoved)
        {
            var stillRemoved = origin.Grants.Values.Any(other => other != removed &&
                other.FactionsRemoved.Contains(faction));
            if (!stillRemoved)
                _factions.AddFaction(body, faction.Id);
        }

        foreach (var other in origin.Grants.Values)
        {
            if (other == removed)
                continue;

            foreach (var faction in other.FactionsAdded)
                _factions.AddFaction(body, faction.Id);
            foreach (var faction in other.FactionsRemoved)
                _factions.RemoveFaction(body, faction.Id);
        }
    }

    private string RoleName(string roleId)
    {
        var key = roleId switch
        {
            RemoveAntagRoles.Traitor => "ss220-removeantag-role-traitor",
            RemoveAntagRoles.Thief => "ss220-removeantag-role-thief",
            RemoveAntagRoles.Cult => "ss220-removeantag-role-cult",
            RemoveAntagRoles.InitialInfected => "ss220-removeantag-role-initial-infected",
            RemoveAntagRoles.HeadRevolutionary => "ss220-removeantag-role-headrev",
            _ => null
        };

        return key is null ? roleId : Loc.GetString(key);
    }
}
