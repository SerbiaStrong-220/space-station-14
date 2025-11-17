// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.SS220.StationEvents.Components;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Robust.Shared.Random;

namespace Content.Server.SS220.StationEvents.Events;

public sealed class AdditionalRulesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdditionalRulesComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<AdditionalRulesComponent> ent, ref ComponentInit args)
    {
        foreach (var kvp in ent.Comp.Rules)
        {
            var rule = _entityTable.GetSpawns(kvp.Value);

            _ticker.AddGameRule(kvp.Key);

            _adminLogger.Add(LogType.EventStarted,
                $"{ToPrettyString(ent):entity} added a game rule [{kvp.Key}]" +
                $" via a chance on AdditionalRulesComponent.");
        }
    }
}
