// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.SS220.Objectives.Components;
using Content.Server.SS220.Trackers.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.SSDIndicator;

namespace Content.Server.SS220.Objectives.Systems;

public sealed partial class IntimidatePersonConditionSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private TargetObjectiveSystem _targetObjective = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntimidatePersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<IntimidatePersonConditionComponent, ObjectiveAssignedEvent>(OnPersonAssigned);
        SubscribeLocalEvent<IntimidatePersonConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnGetProgress(Entity<IntimidatePersonConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        if (!_targetObjective.GetTarget(entity.Owner, out _))
            return;

        if (entity.Comp.ObjectiveIsDone)
        {
            args.Progress = 1f;
            return;
        }

        //HandleSSDMoment
        if (!TryComp<SSDIndicatorComponent>(entity.Comp.TargetMob, out var ssdIndicator)
            || ssdIndicator.IsSSD)
        {
            args.Progress = 1f;
            SetDescription(entity, IntimidatePersonDescriptionType.SSD);
            return;
        }

        SetDescription(entity, IntimidatePersonDescriptionType.Start);
        args.Progress = GetProgress(entity.Comp.TargetMob);
        if (args.Progress >= 1f)
        {
            entity.Comp.ObjectiveIsDone = true;
            SetDescription(entity, IntimidatePersonDescriptionType.Success);
        }
    }

    private void OnPersonAssigned(Entity<IntimidatePersonConditionComponent> entity, ref ObjectiveAssignedEvent args)
    {
        args.Cancelled = true;
        var (uid, _) = entity;

        if (args.Mind.CurrentEntity == null)
            return;

        if (!TryComp<TargetObjectiveComponent>(uid, out var targetObjectiveComponent))
            return;

        if (targetObjectiveComponent.Target != null)
            return;

        if (_target.PickFromPool(entity.Comp.Pool, entity.Comp.Filter, args.MindId) is not { } picked)
            return;

        var target = picked.Comp.OwnedEntity;

        if (target == null)
            return;

        args.Cancelled = false;
        _targetObjective.SetTarget(uid, picked.Owner, targetObjectiveComponent);
        var damageReceivedTracker = AddComp<DamageReceivedTrackerComponent>(target.Value);
        entity.Comp.TargetMob = target.Value;
        damageReceivedTracker.WhomDamageTrack = args.Mind.CurrentEntity.Value;
        damageReceivedTracker.DamageTracker = entity.Comp.DamageTrackerSpecifier;
    }

    private void OnAfterAssign(Entity<IntimidatePersonConditionComponent> entity, ref ObjectiveAfterAssignEvent args)
    {
        if (entity.Comp.StartDescription != null)
            _metaData.SetEntityDescription(entity.Owner, Loc.GetString(entity.Comp.StartDescription));
    }

    private float GetProgress(EntityUid target, DamageReceivedTrackerComponent? tracker = null)
    {
        if (!Resolve(target, ref tracker))
            return 0f;

        return tracker.GetProgress();
    }

    /// <summary>
    /// A way to change description mindlessly
    /// </summary>
    private void SetDescription(Entity<IntimidatePersonConditionComponent> entity, IntimidatePersonDescriptionType type)
    {
        var (uid, component) = entity;
        if (component.DescriptionType == type)
            return;

        var newDescription = type switch
        {
            IntimidatePersonDescriptionType.Start => component.StartDescription,
            IntimidatePersonDescriptionType.Success => component.SuccessDescription,
            IntimidatePersonDescriptionType.SSD => component.SSDDescription,
            _ => null
        };

        if (newDescription == null)
            return;

        _metaData.SetEntityDescription(uid, Loc.GetString(newDescription));
    }
}
