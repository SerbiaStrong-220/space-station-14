using Content.Server.Mind;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.SS220.Objectives;
using Robust.Shared.Player;

namespace Content.Server.SS220.Objectives.Systems;

public sealed class ToggleObjectiveStatusSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<ToggleObjectiveStatusEvent>(OnToggleObjective);
    }

    private void OnToggleObjective(ToggleObjectiveStatusEvent ev)
    {
        var user = GetEntity(ev.Target);

        if (!_mind.TryGetMind(user, out var mind, out var mindComp))
            return;

        if (mindComp.Objectives.Count == 0)
            return;

        EntityUid? toggledObjective = null;

        foreach (var objective in mindComp.Objectives)
        {
            if (!TryComp<ObjectiveComponent>(objective, out var objectiveComp))
                continue;

            var info = _objectives.GetInfo(objective, mind);
            if (info == null)
                continue;

            if (!info.Equals(ev.ObjectiveInfo))
                continue;

            SharedObjectivesSystem.ToggleCompleted((objective, objectiveComp));
            toggledObjective = objective;
            break;
        }

        if (toggledObjective == null)
            return;

        var admin = GetEntity(ev.Admin);
        if (!TryComp<ActorComponent>(admin, out var actor))
            return;

        RaiseNetworkEvent(new UpdateAntagonistInfoEvent(ev.Target), actor.PlayerSession);

        _adminLog.Add(LogType.AdminCommand,
            $"Admin {ToPrettyString(admin)} toggled objective: {ToPrettyString(toggledObjective)} for user: {ToPrettyString(user)}");
    }
}
