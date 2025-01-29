using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class VentCrittersRule : StationEventSystem<VentCrittersRuleComponent>
{
    /*
     * DO NOT COPY PASTE THIS TO MAKE YOUR MOB EVENT.
     * USE THE PROTOTYPE.
     */

    protected override void Started(EntityUid uid, VentCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            return;
        }
        //SS220 fauna update
        if (component.StackAmount == 0)
        {
            return;
        }
        //SS220 fauna update
        var locations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();
        var counter = 1;
        while (locations.MoveNext(out _, out _, out var transform))
        {
            if (counter % component.StackAmount == 0)
            {
                if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station &&
                HasComp<BecomesStationComponent>(transform.GridUid)) //SS220 Vent critters spawn fix
                {
                    validLocations.Add(transform.Coordinates);
                    foreach (var spawn in EntitySpawnCollection.GetSpawns(component.Entries, RobustRandom))
                    {
                        for (var i = 0; i < component.StackAmount; i++) //SS220 fauna Update for loop
                        {
                            Spawn(spawn, transform.Coordinates); //This line from offs
                        }
                    }
                }
            }
            counter = counter + 1;
        }

        if (component.SpecialEntries.Count == 0 || validLocations.Count == 0)
        {
            return;
        }

        // guaranteed spawn
        var specialEntry = RobustRandom.Pick(component.SpecialEntries);
        var specialSpawn = RobustRandom.Pick(validLocations);
        Spawn(specialEntry.PrototypeId, specialSpawn);

        foreach (var location in validLocations)
        {
            foreach (var spawn in EntitySpawnCollection.GetSpawns(component.SpecialEntries, RobustRandom))
            {
                Spawn(spawn, location);
            }
        }
    }
}
