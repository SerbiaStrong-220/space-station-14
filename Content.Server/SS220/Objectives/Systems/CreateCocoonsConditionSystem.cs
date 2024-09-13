// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Objectives.Systems;
using Content.Server.SS220.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.SpiderQueen.Components;

namespace Content.Server.SS220.Objectives.Systems;

public sealed partial class CreateCocoonsConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    private Dictionary<MindComponent, bool> IsCompletedOnce = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreateCocoonsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<CreateCocoonsConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind, _number.GetTarget(ent.Owner));
    }

    private float GetProgress(MindComponent mind, int target)
    {
        if (IsCompletedOnce.TryGetValue(mind, out var comleted) &&
            comleted)
            return 1f;

        var mobUid = mind.CurrentEntity;
        if (mobUid is null ||
            !TryComp<SpiderQueenComponent>(mobUid, out var spiderQueen))
            return 0f;

        if (spiderQueen.CocoonsList.Count >= target)
        {
            IsCompletedOnce.Add(mind, true);
            return 1f;
        }
        else
        {
            return 0f;
        }
    }
}
