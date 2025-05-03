using Content.Server.Mind;
using Content.Shared.Examine;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Whitelist;

namespace Content.Server.Corvax.HiddenDescription;

public sealed partial class HiddenDescriptionSystem : EntitySystem
{

    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiddenDescriptionComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HiddenDescriptionComponent> hiddenDesc, ref ExaminedEvent args)
    {
        PushExamineInformation(hiddenDesc.Comp, ref args);
    }

    public void PushExamineInformation(HiddenDescriptionComponent component, ref ExaminedEvent args)
    // SS220-fix-hidden-desc-fix-end
    {
        _mind.TryGetMind(args.Examiner, out var mindId, out var mindComponent);

        foreach (var item in component.Entries)
        {
            var isJobAllow = false;
            if (_roles.MindHasRole<JobRoleComponent>((mindId, mindComponent), out var jobRole))
            {
                isJobAllow = jobRole.Value.Comp1.JobPrototype != null &&
                             item.JobRequired.Contains(jobRole.Value.Comp1.JobPrototype.Value);
            }

            var isMindWhitelistPassed = MindRoleCheckPass(item.WhitelistMindRoles, mindId);
            var isBodyWhitelistPassed = _whitelist.IsValid(item.WhitelistBody, args.Examiner);
            var passed = item.NeedAllCheck
                ? isMindWhitelistPassed && isBodyWhitelistPassed && isJobAllow
                : isMindWhitelistPassed || isBodyWhitelistPassed || isJobAllow;

            if (passed)
                args.PushMarkup(Loc.GetString(item.Label), component.PushPriority);
        }
    }

    private bool MindRoleCheckPass(HashSet<string> roles, EntityUid mind)
    {
        foreach (var role in roles)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(role, out var roleReg))
            {
                Log.Error($"Role component not found for RoleRequirementComponent: {role}");
                continue;
            }

            if (_roles.MindHasRole(mind, roleReg.Type, out _))
                return true;
        }

        return false;
    }
}
