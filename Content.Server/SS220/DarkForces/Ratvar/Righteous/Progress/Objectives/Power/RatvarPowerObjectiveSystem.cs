using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress.Objectives.Power;

public sealed class RatvarPowerObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly RatvarProgressSystem _progressSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarPowerObjectiveComponent, GroupObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<RatvarPowerObjectiveComponent, GroupObjectiveAfterAssignEvent>(OnAfterAssigned);
        SubscribeLocalEvent<RatvarPowerObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, RatvarPowerObjectiveComponent component,
        ref ObjectiveGetProgressEvent args)
    {
        var progress = (float)_progressSystem.GetCurrentPower() / component.RequiredCount;
        if (progress >= 1f)
        {
            progress = 1f;
        }

        args.Progress = progress;
    }

    private void OnAfterAssigned(EntityUid uid, RatvarPowerObjectiveComponent component,
        ref GroupObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid,
            $"Накопите {component.RequiredCount} мощи");
    }

    private void OnAssigned(EntityUid uid, RatvarPowerObjectiveComponent component,
        ref GroupObjectiveAssignedEvent args)
    {
        var count = _robustRandom.Next(15000, 20000);
        component.RequiredCount = Math.DivRem(count, 1000).Quotient * 1000;
    }
}
