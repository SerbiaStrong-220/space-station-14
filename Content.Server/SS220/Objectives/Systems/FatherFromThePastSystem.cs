using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class FatherFromThePastConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FatherFromThePastConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<FatherFromThePastConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(Entity<FatherFromThePastConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        var childName = GetChildBody(args.Mind) is { } child
            ? Name(child)
            : Loc.GetString("objective-father-from-the-past-unknown-child");

        _metaData.SetEntityName(ent.Owner, Loc.GetString("objective-father-from-the-past-title", ("targetName", childName)));
    }

    private void OnGetProgress(Entity<FatherFromThePastConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = IsEscapedAlive(args.Mind.OwnedEntity) && IsEscapedAlive(GetChildBody(args.Mind)) ? 1f : 0f;
    }

    private EntityUid? GetChildBody(MindComponent fatherMind)
    {
        if (fatherMind.OwnedEntity is not { } fatherBody ||
            !TryComp<TargetOverrideComponent>(fatherBody, out var ovr) ||
            ovr.Target is not { } childMindId ||
            !TryComp<MindComponent>(childMindId, out var childMind))
            return null;

        return childMind.OwnedEntity;
    }

    private bool IsEscapedAlive(EntityUid? body) =>
        body is { } uid && !Deleted(uid) && _mobState.IsAlive(uid) && _emergencyShuttle.IsTargetEscaping(uid);
}
