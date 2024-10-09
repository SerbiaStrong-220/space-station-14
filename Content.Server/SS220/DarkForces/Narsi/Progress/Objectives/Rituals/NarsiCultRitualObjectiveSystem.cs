using System.Linq;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.DarkForces.Narsi.Progress.Objectives;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Rituals;

public sealed class NarsiCultRitualObjectiveSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiCultRitualObjectiveComponent, GroupObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<NarsiCultRitualObjectiveComponent, GroupObjectiveAfterAssignEvent>(OnAfterAssigned);

        SubscribeLocalEvent<NarsiRitualCompletedEvent>(OnRitualCompleted);
    }

    private void OnAfterAssigned(EntityUid uid, NarsiCultRitualObjectiveComponent component,
        ref GroupObjectiveAfterAssignEvent args)
    {
        var title = component.RequiredRitual?.Name;
        if (title == null)
            return;

        _metaData.SetEntityName(uid, $"Проведите ритуал: {title}", args.Meta);
    }

    private void OnRitualCompleted(ref NarsiRitualCompletedEvent args)
    {
        var query = EntityQuery<NarsiCultRitualObjectiveComponent>().ToList();
        foreach (var component in query)
        {
            if (component.RequiredRitual?.ID != args.Ritual)
                continue;

            RaiseLocalEvent(new NarsiCultObjectiveCompleted(component.Owner));
        }
    }

    private void OnAssigned(EntityUid uid, NarsiCultRitualObjectiveComponent component,
        ref GroupObjectiveAssignedEvent args)
    {
        var ritualProto = _robustRandom.Pick(component.Rituals);
        if (!_prototypeManager.TryIndex(ritualProto, out var ritual))
        {
            args.Cancelled = true;
            args.Reason = "Не найден прототип для ритуала " + ritualProto.Id;
            return;
        }

        component.RequiredRitual = ritual;
    }
}
