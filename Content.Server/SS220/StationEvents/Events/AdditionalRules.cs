// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.SS220.StationEvents.Components;
using Content.Server.StationEvents;
using Content.Shared.Database;

namespace Content.Server.SS220.StationEvents.Events;

public sealed class AdditionalRulesSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdditionalRulesComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<AdditionalRulesComponent> ent, ref ComponentInit args)
    {
        foreach (var kvp in ent.Comp.Rules)
        {
            var table = kvp.Value;
            if (table == null)
                continue;

            _event.RunRandomEvent(table);

            _adminLogger.Add(LogType.EventRan,
                $"{ToPrettyString(ent):event} additionally tried to run the EntityTableSelector [{kvp.Key}]" +
                $" via an AdditionalRulesComponent.");
        }
    }
}
