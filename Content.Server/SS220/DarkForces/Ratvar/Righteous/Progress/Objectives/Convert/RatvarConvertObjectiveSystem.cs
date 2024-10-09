using System.Linq;
using Content.Server.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Roles;
using Robust.Shared.Random;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress.Objectives.Convert;

public sealed class RatvarConvertObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarConvertObjectiveComponent, GroupObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<RatvarConvertObjectiveComponent, GroupObjectiveAfterAssignEvent>(OnAfterAssigned);
        SubscribeLocalEvent<RatvarConvertObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, RatvarConvertObjectiveComponent component,
        ref ObjectiveGetProgressEvent args)
    {
        var righteouses = EntityQuery<RatvarRighteousComponent>();
        var maradeurs = EntityQuery<RatvarMarauderShellComponent>().Where(marouder => _mindSystem.TryGetMind(marouder.Owner, out _, out _));

        var progress = (righteouses.Count() + maradeurs.Count()) / component.RequiredCount;
        if (progress >= 1f)
        {
            progress = 1f;
        }

        args.Progress = progress;
    }

    private void OnAfterAssigned(EntityUid uid, RatvarConvertObjectiveComponent component,
        ref GroupObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid,
            $"Усильте культ новыми членами. Необходимо {component.RequiredCount} праведников или мародёров");
    }

    private void OnAssigned(EntityUid uid, RatvarConvertObjectiveComponent component,
        ref GroupObjectiveAssignedEvent args)
    {
        component.RequiredCount = _robustRandom.Next(4, 7);
    }
}
