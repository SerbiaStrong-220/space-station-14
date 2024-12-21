// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Objectives.Systems;
using Content.Server.Roles;
using Content.Server.SS220.Objectives.Components;
using Content.Server.SS220.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.SpiderQueen.Components;

namespace Content.Server.SS220.Objectives.Systems;

public sealed partial class CreateCocoonsConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreateCocoonsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<CreateCocoonsConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind, _number.GetTarget(ent.Owner));
    }

    private float GetProgress(EntityUid mindId, MindComponent mind, int target)
    {
        if (!_role.MindHasRole<SpiderQueenRoleComponent>(mindId, out var spiderQueenRole))
            return 0f;

        if (spiderQueenRole.Value.Comp2.IsCreateCocoonsCompletedOnce)
            return 1f;

        var mobUid = mind.CurrentEntity;
        if (mobUid is null ||
            !TryComp<SpiderQueenComponent>(mobUid, out var spiderQueen))
            return 0f;

        var progress = spiderQueen.CocoonsList.Count >= target
            ? 1f
            : (float)spiderQueen.CocoonsList.Count / (float)target;

        if (progress == 1f)
            spiderQueenRole.Value.Comp2.IsCreateCocoonsCompletedOnce = true;

        return progress;
    }
}
