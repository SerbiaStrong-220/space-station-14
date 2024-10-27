using Content.Server.Mind;
using Content.Shared.Examine;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Whitelist;

namespace Content.Server.Corvax.HiddenDescription;

public sealed partial class HiddenDescriptionSystem : EntitySystem
{

    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!; //SS220

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiddenDescriptionComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HiddenDescriptionComponent> hiddenDesc, ref ExaminedEvent args)
    {
        _mind.TryGetMind(args.Examiner, out var mindId, out var mindComponent);
        _mind.TryGetRole<MindRoleComponent>(args.Examiner, out var role);

        foreach (var item in hiddenDesc.Comp.Entries)
        {
            var isJobAllow = role?.JobPrototype != null && item.JobRequired.Contains(role.JobPrototype.Value);
            var isMindWhitelistPassed = _whitelist.IsValid(item.WhitelistMind, mindId);
            var isBodyWhitelistPassed = _whitelist.IsValid(item.WhitelistMind, args.Examiner);
            var passed = item.NeedAllCheck
                ? isMindWhitelistPassed && isBodyWhitelistPassed && isJobAllow
                : isMindWhitelistPassed || isBodyWhitelistPassed || isJobAllow;

            if (passed)
                args.PushMarkup(Loc.GetString(item.Label), hiddenDesc.Comp.PushPriority);
        }
    }
}
