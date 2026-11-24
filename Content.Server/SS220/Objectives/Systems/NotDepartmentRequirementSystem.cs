// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;

namespace Content.Server.SS220.Objectives.Systems;

/// <summary>
/// Checks an objective's department blacklist against the assigning mind's job.
/// </summary>
public sealed class NotDepartmentRequirementSystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NotDepartmentRequirementComponent, RequirementCheckEvent>(OnRequirementCheckEvent);
    }

    private void OnRequirementCheckEvent(Entity<NotDepartmentRequirementComponent> ent, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_jobs.MindTryGetJob(args.MindId, out var job))
            return;

        if (!_jobs.TryGetAllDepartments(job.ID, out var departments))
            return;

        foreach (var department in departments)
        {
            if (!ent.Comp.Departments.Contains(department.ID))
                continue;

            args.Cancelled = true;
            return;
        }
    }
}
