using Content.Server.EUI;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.SS220.Pirates;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pirates;

public sealed partial class PirateRecruitmentSystem : EntitySystem
{
    private static readonly ProtoId<NpcFactionPrototype> PirateFaction = "Syndicate";
    private static readonly ProtoId<JobPrototype> BrigmedicJob = "Brigmedic";
    private static readonly HashSet<ProtoId<DepartmentPrototype>> ForbiddenDepartments =
    [
        "Command",
        "Security",
        "Silicon",
    ];

    [Dependency] private EuiManager _eui = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _roles = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private NpcFactionSystem _factions = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PirateRecruitmentContractComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PirateRecruitmentContractComponent, PirateRecruitmentDoAfterEvent>(OnRecruitmentDoAfter);
    }

    private void OnAfterInteract(Entity<PirateRecruitmentContractComponent> contract, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target ||
            !args.CanReach ||
            contract.Comp.OfferedTarget is not null ||
            !IsPirate(args.User) ||
            !CanRecruit(target, out _))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, contract.Comp.RecruitmentDelay,
            new PirateRecruitmentDoAfterEvent(), contract, target: target, used: contract)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnRecruitmentDoAfter(Entity<PirateRecruitmentContractComponent> contract,
        ref PirateRecruitmentDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target ||
            contract.Comp.OfferedTarget is not null ||
            !IsPirate(args.User) ||
            !CanRecruit(target, out _) ||
            !_players.TryGetSessionByEntity(target, out var session))
        {
            return;
        }

        contract.Comp.Offerer = args.User;
        contract.Comp.OfferedTarget = target;
        _eui.OpenEui(new PirateRecruitmentEui(contract.Owner, target, this), session);
        args.Handled = true;
    }

    public void RespondToOffer(EntityUid contractUid, EntityUid target, bool accepted)
    {
        if (!TryComp(contractUid, out PirateRecruitmentContractComponent? contract) ||
            contract.OfferedTarget != target)
            return;

        var offerer = contract.Offerer;
        if (!accepted || offerer is null || !IsPirate(offerer.Value) || !CanRecruit(target, out var rule))
        {
            ClearOffer(contractUid, target);
            return;
        }

        if (!_mind.TryGetMind(target, out var mind, out var mindComponent))
        {
            ClearOffer(contractUid, target);
            return;
        }

        _roles.MindAddRole(mind, "MindRolePirateExpansion", mindComponent);
        _mind.TryAddObjective(mind, mindComponent, "PirateLootValueObjective");
        _mind.TryAddObjective(mind, mindComponent, "PirateCrewCaptureObjective");
        var faction = EnsureComp<NpcFactionMemberComponent>(target);
        _factions.ClearFactions((target, faction), false);
        _factions.AddFaction((target, faction), PirateFaction);
        rule.Comp.SuccessfulRecruits++;
        _adminLogger.Add(LogType.Mind, LogImpact.Medium,
            $"{ToPrettyString(target):player} accepted pirate recruitment offered by {ToPrettyString(offerer.Value):player}");
        ClearOffer(contractUid, target);
    }

    public void ClearOffer(EntityUid contractUid, EntityUid target)
    {
        if (!TryComp(contractUid, out PirateRecruitmentContractComponent? contract) ||
            contract.OfferedTarget != target)
        {
            return;
        }

        contract.Offerer = null;
        contract.OfferedTarget = null;
    }

    private bool CanRecruit(EntityUid target, out Entity<PirateGameRuleComponent> rule)
    {
        if (!TryGetRule(out rule) ||
            rule.Comp.SuccessfulRecruits >= rule.Comp.MaximumRecruits ||
            !HasComp<HumanoidProfileComponent>(target) ||
            HasComp<SiliconLawBoundComponent>(target) ||
            !TryComp<MobStateComponent>(target, out var mobState) ||
            !_mobState.IsAlive(target, mobState) ||
            !_mind.TryGetMind(target, out var mind, out _))
        {
            return false;
        }

        if (_roles.MindIsAntagonist(mind) ||
            !_jobs.MindTryGetJobId(mind, out var job) ||
            job is null ||
            job == BrigmedicJob)
        {
            return false;
        }

        if (!_jobs.TryGetAllDepartments(job.Value, out var departments))
            return true;

        foreach (var department in departments)
        {
            if (ForbiddenDepartments.Contains(department.ID))
                return false;
        }

        return true;
    }

    private bool IsPirate(EntityUid entity)
    {
        return _mind.TryGetMind(entity, out var mind, out _) &&
               _roles.MindHasRole<PirateCrewRoleComponent>(mind);
    }

    private bool TryGetRule(out Entity<PirateGameRuleComponent> rule)
    {
        var query = EntityQueryEnumerator<PirateGameRuleComponent, ActiveGameRuleComponent>();
        if (query.MoveNext(out var uid, out var component, out _))
        {
            rule = (uid, component);
            return true;
        }

        rule = default;
        return false;
    }
}
