using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Progress.Components;
using Content.Server.SS220.DarkForces.Saint.Chaplain.Components;
using Content.Server.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.DarkForces.Narsi.Progress.Objectives;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Robust.Shared.Random;
using Content.Shared.Objectives.Components;

namespace Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Offering;

public sealed class NarsiCultOfferObjectiveSystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiCultOfferObjectiveComponent, GroupObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<NarsiCultOfferingTargetComponent, NarsiCultOfferingTargetEvent>(OnOfferingTarget);
    }

    private void OnOfferingTarget(EntityUid uid, NarsiCultOfferingTargetComponent component, ref NarsiCultOfferingTargetEvent args)
    {
        foreach (var objective in component.Objectives)
        {
            RaiseLocalEvent(new NarsiCultObjectiveCompleted(objective));
        }
    }

    private void OnAssigned(EntityUid uid, NarsiCultOfferObjectiveComponent component, ref GroupObjectiveAssignedEvent args)
    {
        var allHumans = _mind.GetAliveHumans()
            .Where(entity => !HasComp<NarsiCultistComponent>(entity) && !HasComp<ChaplainComponent>(entity))
            .ToList();

        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            args.Reason = "Не найдено ни одного человека, кто мог бы быть корректной целью культа";
            return;
        }

        var target = _random.Pick(allHumans);
        var objective = (uid, component);

        SetupOfferingTarget(objective, target);

        var title = GetObjectiveTitle(objective, target);
        _metaData.SetEntityName(uid, title);
    }

    private void SetupOfferingTarget(Entity<NarsiCultOfferObjectiveComponent> objective, EntityUid target)
    {
        var targetComp = EnsureComp<NarsiCultOfferingTargetComponent>(target);
        targetComp.Objectives.Add(objective);

        objective.Comp.Target = target;
    }

    private string GetObjectiveTitle(Entity<NarsiCultOfferObjectiveComponent> objective, EntityUid target)
    {
        var objectiveMeta = MetaData(objective);
        var targetName = "Неизвестно";

        if (_mind.TryGetMind(target, out var mindId, out var mind) && mind.CharacterName != null)
        {
            targetName = mind.CharacterName;
        }

        var jobName = _job.MindTryGetJobName(mindId);
        return $"{objectiveMeta.EntityName}: {targetName} ({jobName})";
    }
}
