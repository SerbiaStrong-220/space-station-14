using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
﻿using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Content.Server.SS220.Hop;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class BureaucraticErrorRule : StationEventSystem<BureaucraticErrorRuleComponent>
{
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    protected override void Started(EntityUid uid, BureaucraticErrorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation, HasComp<StationJobsComponent>))
            return;

        var jobList = _stationJobs.GetJobs(chosenStation.Value).Keys.ToList();

        foreach(var job in component.IgnoredJobs)
            jobList.Remove(job);

        if (jobList.Count == 0)
            return;

        // SS220 Hop improve begin: track actual changes, not merely randomly selected roles.
        var before = _stationJobs.GetJobs(chosenStation.Value).ToDictionary(x => x.Key, x => x.Value);
        ProtoId<JobPrototype>? preferredJob = null;
        // SS220 Hop improve end
        // Low chance to completely change up the late-join landscape by closing all positions except infinite slots.
        // Lower chance than the /tg/ equivalent of this event.
        if (RobustRandom.Prob(0.25f))
        {
            var chosenJob = RobustRandom.PickAndTake(jobList);
            preferredJob = chosenJob; // SS220 Hop improve
            _stationJobs.MakeJobUnlimited(chosenStation.Value, chosenJob); // INFINITE chaos.
            foreach (var job in jobList)
            {
                if (_stationJobs.IsJobUnlimited(chosenStation.Value, job))
                    continue;
                _stationJobs.TrySetJobSlot(chosenStation.Value, job, 0);
            }
        }
        else
        {
            var lower = (int) (jobList.Count * 0.20);
            var upper = (int) (jobList.Count * 0.30);
            // Changing every role is maybe a bit too chaotic so instead change 20-30% of them.
            var num = RobustRandom.Next(lower, upper);
            for (var i = 0; i < num; i++)
            {
                var chosenJob = RobustRandom.PickAndTake(jobList);
                if (_stationJobs.IsJobUnlimited(chosenStation.Value, chosenJob))
                    continue;

                _stationJobs.TryAdjustJobSlot(chosenStation.Value, chosenJob, RobustRandom.Next(-3, 6), clamp: true);
            }
        }

        // SS220 Hop improve begin: protect event-modified limits from the crew management console.
        var changed = _stationJobs.GetJobs(chosenStation.Value)
            .Where(x => !before.TryGetValue(x.Key, out var old) || old != x.Value)
            .Select(x => x.Key).ToHashSet();
        // The chosen unlimited role is affected even if it was unlimited before this event.
        if (preferredJob is { } preferred)
            changed.Add(preferred);
        if (changed.Count > 0)
            RaiseLocalEvent(new BureaucraticJobsChangedEvent(chosenStation.Value, changed, preferredJob));
        // SS220 Hop improve end
    }
}
