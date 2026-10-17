// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.IntegrationTests.Fixtures;
using Content.Server.Atmos.Rotting;
using Content.Shared.Atmos.Rotting;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.SS220.Tests.Atmos;

public sealed class RottingContainerTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: FoodMeatBear
  id: ForcedRottingMeat
  components:
  - type: Perishable
    forceRotProgression: true
";

    [TestCase("FoodMeatBear", false)]
    [TestCase("ForcedRottingMeat", true)]
    public async Task NestedContainerPreservation(string meatPrototype, bool forceRotProgression)
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var containers = SEntMan.System<SharedContainerSystem>();
            var rotting = SEntMan.System<RottingSystem>();

            var meat = SEntMan.SpawnEntity(meatPrototype, map.MapCoords);
            var perishable = SEntMan.GetComponent<PerishableComponent>(meat);
            var preservedEntity = SEntMan.SpawnEntity(null, map.MapCoords);
            SEntMan.AddComponent<AntiRottingContainerComponent>(preservedEntity);
            var preserved = containers.EnsureContainer<Container>(preservedEntity, "preserved");
            var outerEntity = SEntMan.SpawnEntity(null, map.MapCoords);
            var outer = containers.EnsureContainer<Container>(outerEntity, "outer");
            var topEntity = SEntMan.SpawnEntity(null, map.MapCoords);
            var top = containers.EnsureContainer<Container>(topEntity, "top");

            Assert.That(rotting.IsRotProgressing(meat, perishable), Is.True,
                "Raw meat on the floor should perish.");

            Assert.That(containers.Insert(meat, preserved), Is.True);
            Assert.That(rotting.IsRotProgressing(meat, perishable), Is.EqualTo(forceRotProgression),
                "A preserving container should stop decay unless progression is forced.");

            Assert.That(containers.Insert(preservedEntity, outer), Is.True);
            Assert.That(rotting.IsRotProgressing(meat, perishable), Is.EqualTo(forceRotProgression),
                "Putting a preserving container inside an ordinary container should retain preservation.");

            Assert.That(containers.Insert(outerEntity, top), Is.True);
            Assert.That(rotting.IsRotProgressing(meat, perishable), Is.EqualTo(forceRotProgression),
                "Preservation should work through multiple ordinary containers.");

            Assert.That(containers.Insert(meat, outer), Is.True);
            Assert.That(rotting.IsRotProgressing(meat, perishable), Is.True,
                "Meat moved out of the preserving container should resume decaying.");

            Assert.That(containers.Remove(meat, outer, destination: map.GridCoords), Is.True);
            Assert.That(rotting.IsRotProgressing(meat, perishable), Is.True,
                "Meat removed onto the floor should resume decaying.");
        });
    }
}
