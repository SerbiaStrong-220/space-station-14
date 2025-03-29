// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.SS220.Objectives.Components;
using Content.Server.SS220.Trackers.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.SSDIndicator;
using Robust.Shared.Random;

namespace Content.Server.SS220.Objectives.Systems;

public sealed class IntimidatePersonConditionSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntimidatePersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<IntimidatePersonConditionComponent, ObjectiveAssignedEvent>(OnPersonAssigned);
        SubscribeLocalEvent<IntimidatePersonConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
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
        var (uid, _) = entity;

        if (!TryComp<TargetObjectiveComponent>(uid, out var targetObjectiveComponent))
        {
            args.Cancelled = true;
            return;
        }

        if (targetObjectiveComponent.Target != null)
            return;

        var targetableMinds = _mind.GetAliveHumans(args.MindId)
                    .Where(x => TryComp<MindComponent>(x, out var mindComponent)
                                && !HasComp<DamageReceivedTrackerComponent>(GetEntity(mindComponent.OriginalOwnedEntity)))
                    .ToList();

        if (targetableMinds.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var targetMindUid = _random.Pick(targetableMinds);
        var target = GetMindsOriginalEntity(targetMindUid);

        if (args.Mind.CurrentEntity == null
            || target == null)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(uid, targetMindUid, targetObjectiveComponent);
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

    private EntityUid? GetMindsOriginalEntity(EntityUid mindUid)
    {
        return GetEntity(Comp<MindComponent>(mindUid).OriginalOwnedEntity);
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
