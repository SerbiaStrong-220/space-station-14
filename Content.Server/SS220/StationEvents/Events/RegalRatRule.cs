using Content.Server.Station.Components;
using Content.Server.SS220.StationEvents.Events;
using Content.Server.SS220.StationEvents.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.SS220.StationEvents.Events;

public sealed class RegalRatRule : StationEventSystem<RegalRatRuleComponent>
{
    
    protected override void Started(EntityUid uid, RegalRatRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            return;
        }

        var kingRatLocations = EntityQueryEnumerator<RegalRatSpawnLocationComponent, TransformComponent>();
        var mouseLocations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();

        var kingRatValidLocations = new List<EntityCoordinates>();
        var mouseVaidLocations = new List<EntityCoordinates>();

        while (mouseLocations.MoveNext(out _, out _, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station &&
                HasComp<BecomesStationComponent>(transform.GridUid))
            {
                mouseVaidLocations.Add(transform.Coordinates);
                foreach (var spawn in EntitySpawnCollection.GetSpawns(component.Entries, RobustRandom))
                {
                    Spawn(spawn, transform.Coordinates);
                }
            }
        }

        while (kingRatLocations.MoveNext(out _, out _, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station &&
                HasComp<BecomesStationComponent>(transform.GridUid))
            {
                kingRatValidLocations.Add(transform.Coordinates);
            }
        }
        
        if (component.SpecialEntries.Count == 0 || kingRatValidLocations.Count == 0)
        {
            return;
        }

        // guaranteed spawn
        var specialEntry = RobustRandom.Pick(component.SpecialEntries);
        var specialSpawn = RobustRandom.Pick(kingRatValidLocations);
        Spawn(specialEntry.PrototypeId, specialSpawn);

        foreach (var location in kingRatValidLocations)
        {
            foreach (var spawn in EntitySpawnCollection.GetSpawns(component.SpecialEntries, RobustRandom))
            {
                Spawn(spawn, location);
            }
        }
    }
}
