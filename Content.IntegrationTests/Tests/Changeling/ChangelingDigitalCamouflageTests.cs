// SS220 Changeling
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Coordinates;
using Content.Shared.Silicons.StationAi;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests.Changeling;

[TestFixture]
public sealed class ChangelingDigitalCamouflageTests : GameTest
{
    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task UncontrolledAiDoesNotReceiveCamouflagedEntityIds()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid camouflagedBeforeAi = default;
        EntityUid camouflagedAfterAi = default;
        EntityUid ai = default;

        await server.WaitAssertion(() =>
        {
            camouflagedBeforeAi = entMan.SpawnEntity(null, testMap.GridCoords);
            entMan.EnsureComponent<ChangelingDigitalCamouflageComponent>(camouflagedBeforeAi);

            ai = entMan.SpawnEntity(null, testMap.GridCoords);
            entMan.EnsureComponent<StationAiOverlayComponent>(ai);

            camouflagedAfterAi = entMan.SpawnEntity(null, testMap.GridCoords);
            entMan.EnsureComponent<ChangelingDigitalCamouflageComponent>(camouflagedAfterAi);
        });
        await pair.RunTicksSync(2);

        await server.WaitAssertion(() =>
        {
            var viewer = entMan.GetComponent<StationAiDigitalCamouflageComponent>(ai);
            Assert.Multiple(() =>
            {
                Assert.That(viewer.CamouflagedEntities, Is.Empty,
                    "An entity without an attached station-AI player must not receive stable network IDs.");
                Assert.That(viewer.CamouflagedEntities,
                    Does.Not.Contain(entMan.GetNetEntity(camouflagedBeforeAi)));
                Assert.That(viewer.CamouflagedEntities,
                    Does.Not.Contain(entMan.GetNetEntity(camouflagedAfterAi)));
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task ControlledAiDoesNotReceiveCrossMapCamouflagedEntityIds()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var eyeSystem = entMan.System<EyeSystem>();
        var session = await server.AddDummySession();
        var testMap = await pair.CreateTestMap();
        EntityUid remoteMap = default;
        EntityUid camouflaged = default;
        EntityUid eyeTarget = default;
        EntityUid ai = default;

        await server.WaitAssertion(() =>
        {
            remoteMap = mapSystem.CreateMap(out var remoteMapId);
            var remoteGrid = mapMan.CreateGridEntity(remoteMapId);

            ai = entMan.SpawnEntity(null, testMap.GridCoords);
            entMan.EnsureComponent<EyeComponent>(ai);
            entMan.EnsureComponent<StationAiOverlayComponent>(ai);

            eyeTarget = entMan.SpawnEntity(null, testMap.GridCoords);
            camouflaged = entMan.SpawnEntity(null, remoteGrid.Owner.ToCoordinates());
            entMan.EnsureComponent<ChangelingDigitalCamouflageComponent>(camouflaged);

            server.PlayerMan.SetAttachedEntity(session, ai);
            eyeSystem.SetTarget(ai, eyeTarget);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ActorComponent>(ai), Is.True);
                Assert.That(session.ViewSubscriptions, Does.Contain(eyeTarget));
                Assert.That(entMan.GetComponent<TransformComponent>(camouflaged).MapID,
                    Is.Not.EqualTo(entMan.GetComponent<TransformComponent>(eyeTarget).MapID));
            });
        });
        await pair.RunSeconds(0.2f);

        await server.WaitAssertion(() =>
        {
            var viewer = entMan.GetComponent<StationAiDigitalCamouflageComponent>(ai);
            Assert.That(viewer.CamouflagedEntities,
                Does.Not.Contain(entMan.GetNetEntity(camouflaged)),
                "A station AI must never receive a stable network ID for camouflage on another map.");

            entMan.DeleteEntity(remoteMap);
        });
    }
}
