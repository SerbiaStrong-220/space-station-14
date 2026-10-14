// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using System.Numerics;
using Content.Client.SS220.Silicons.StationAi.CameraMap.UI;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.SS220.Silicons.StationAi.CameraMap;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.SS220.Tests.StationAi;

public sealed class StationAiCameraMapWindowTest : InteractionTest
{
    [Test]
    public async Task MapShowsMarkersOfOpenTab()
    {
        var camera = new NetEntity(200001);
        var sensor = new NetEntity(200002);
        StationAiCameraMapWindow window = default!;
        await Client.WaitPost(() =>
        {
            window = new StationAiCameraMapWindow();
            window.OpenCentered();
            var targets = new List<StationAiCameraMapTarget> { new(camera, Vector2.Zero, "Camera", true, true) };
            var crew = new List<StationAiCameraMapCrew> { new(sensor, "Crew", "Job", "JobIconPassenger", true, 0, true) };
            var positions = new Dictionary<NetEntity, Vector2> { [sensor] = Vector2.One };
            window.UpdateState(EntityUid.Invalid, new StationAiCameraMapState(NetEntity.Invalid, targets, crew, positions));
        });

        await Client.WaitAssertion(() =>
        {
            Assert.That(TryGetControlFromChildren<StationAiCameraNavMapControl>(_ => true, window, out var map));
            Assert.That(TryGetControlFromChildren<TabContainer>(_ => true, window, out var tabs));
            Assert.That(map!.TrackedEntities.Keys, Is.EquivalentTo(new[] { camera }), "Cameras tab");

            tabs!.CurrentTab = 1;
            Assert.That(map.TrackedEntities.Keys, Is.EquivalentTo(new[] { sensor }), "Crew tab");
            window.Close();
        });
    }
}
