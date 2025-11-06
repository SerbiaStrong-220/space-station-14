// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.SS220.StationEvents.Components;
using Content.Shared.Database;
using Robust.Shared.Random;

namespace Content.Server.SS220.StationEvents.Events;

public sealed class AdditionalRuleChanceSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdditionalRuleChanceComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<AdditionalRuleChanceComponent> ent, ref ComponentInit args)
    {
        foreach (var kvp in ent.Comp.Rules)
        {
            if (!_random.Prob(kvp.Value))
                continue;

            _ticker.AddGameRule(kvp.Key);

            _adminLogger.Add(LogType.EventStarted,
                $"{ToPrettyString(ent):entity} added a game rule [{kvp.Key}]" +
                $" via a chance on AdditionalRuleChanceComponent.");
        }
    }
}
