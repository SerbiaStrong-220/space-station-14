// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.SS220.Objectives.Components;
using Content.Server.SS220.Trackers.Components;
using Content.Shared.Objectives.Components;
using Robust.Server.Containers;
using Robust.Shared.Random;

namespace Content.Server.SS220.Objectives.Systems;

public sealed class IntimidatePersonConditionSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntimidatePersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnPersonAssigned);
    }

    private void OnGetProgress(Entity<IntimidatePersonConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(entity.Owner, out var target))
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
        var (uid, _) = entity;

        if (!TryComp<TargetObjectiveComponent>(uid, out var targetObjectiveComponent))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (targetObjectiveComponent.Target != null)
            return;

        // no other humans to kill
        var allHumans = _mind.GetAliveHumansExcept(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var targetUid = _random.Pick(allHumans);
        if (HasComp<DamageReceivedTrackerComponent>(targetUid)
            || !TryComp<IntimidatePersonConditionComponent>(uid, out var intimidatePerson)
            || args.Mind.CurrentEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(uid, targetUid, targetObjectiveComponent);
        var damageReceivedTracker = AddComp<DamageReceivedTrackerComponent>(targetUid);
        damageReceivedTracker.WhomDamageTrack = args.Mind.CurrentEntity.Value;
        damageReceivedTracker.DamageTracker = intimidatePerson.DamageTrackerSpecifier;
    }

    private float GetProgress(EntityUid target, DamageReceivedTrackerComponent? tracker = null)
    {
        if (!Resolve(target, ref tracker))
            return 0f;

        return tracker.GetProgress();
    }
}
