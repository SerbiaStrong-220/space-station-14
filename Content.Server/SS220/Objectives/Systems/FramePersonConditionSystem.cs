// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.SS220.Objectives.Components;
using Content.Server.SS220.Trackers.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Objectives.Systems;

public sealed class FramePersonConditionSystem : EntitySystem
{
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TargetObjectiveSystem _targetObjective = default!;

    /// <summary>
    /// We use this to determine which jobs have legalImmunity... Wait for MRP PR for a special flag.
    /// </summary>
    private readonly string _legalImmunitySupervisors = "job-supervisors-centcom";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FramePersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnPersonAssigned);
    }

    private void OnGetProgress(Entity<FramePersonConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        if (!_targetObjective.GetTarget(entity.Owner, out var target))
            return;

        if (entity.Comp.ObjectiveIsDone)
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = GetProgress(target.Value);
        if (args.Progress >= 1f)
            entity.Comp.ObjectiveIsDone = true;
    }

    private void OnPersonAssigned(Entity<PickRandomPersonComponent> entity, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<FramePersonConditionComponent>(entity.Owner, out var framePersonCondition))
            return;

        args.Cancelled = !(TryPickRandomPerson(entity.Owner, args.MindId, out var target)
                        && TryTrackIdCardOwner(target.Value, args.MindId, framePersonCondition));
    }

    private bool TryTrackIdCardOwner(EntityUid idCardOwner, EntityUid trackedByMind, FramePersonConditionComponent objective)
    {
        if (!_idCard.TryFindIdCard(idCardOwner, out var idCard))
            return false;

        var criminalStatusTracker = AddComp<CriminalStatusTrackerComponent>(idCard);
        criminalStatusTracker.CriminalStatusSpecifier = objective.CriminalStatusSpecifier;
        criminalStatusTracker.TrackedByMind = trackedByMind;
        return true;
    }

    private bool TryPickRandomPerson(EntityUid objective, EntityUid objectiveOwnerMind, [NotNullWhen(true)] out EntityUid? picked, List<Type>? blacklist = null)
    {
        picked = null;
        if (!TryComp<TargetObjectiveComponent>(objective, out var targetObjective))
            return false;

        if (targetObjective.Target != null)
            return false;

        var whitelistedPlayers = _mind.GetAliveHumansExcept(objectiveOwnerMind)
                                    .Where(x => CorrectJob(x) && (blacklist == null || !EntityHasAnyComponent(x, blacklist)))
                                    .ToList();

        if (whitelistedPlayers.Count == 0)
            return false;

        picked = _random.Pick(whitelistedPlayers);
        _targetObjective.SetTarget(objective, picked.Value, targetObjective);
        return true;
    }

    private bool EntityHasAnyComponent(EntityUid uid, List<Type> whitelist)
    {
        foreach (var type in whitelist)
        {
            if (HasComp(uid, type))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if that job can be framed. Relays on supervisor cause... no RP in code sry.
    /// </summary>
    private bool CorrectJob(EntityUid uid)
    {
        if (!(TryComp<MindRoleComponent>(uid, out var mindRoleComponent)
            && mindRoleComponent.JobPrototype.HasValue))
            return false;

        if (_prototype.Index(mindRoleComponent.JobPrototype.Value).Supervisors == _legalImmunitySupervisors)
            return true;

        return false;
    }

    private float GetProgress(EntityUid target)
    {
        if (!_idCard.TryFindIdCard(target, out var idCard)
            || !TryComp<CriminalStatusTrackerComponent>(idCard, out var statusTrackerComponent))
            return 1f; // Uh... hmmm... fuck....  <- maybe we need to change target at that point. Anyways player have no fault of that.

        return statusTrackerComponent.GetProgress();
    }
}
