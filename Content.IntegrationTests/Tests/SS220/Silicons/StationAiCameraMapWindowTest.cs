// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.SS220.Silicons.StationAi;
using Content.Client.SS220.Silicons.StationAi.UI;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.SS220.Silicons.StationAi;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.SS220.Silicons;

public sealed class StationAiCameraMapWindowTest : InteractionTest
{
    [Test]
    [TestCase(1020, 740, false)]
    [TestCase(640, 740, true)]
    [TestCase(1020, 1200, true)]
    public async Task WindowResizesToRequestedLayout(float width, float height, bool portrait)
    {
        StationAiCameraMapWindow window = default!;
        await Client.WaitPost(() =>
        {
            window = new StationAiCameraMapWindow();
            window.OpenCentered();
            // Long names must clip instead of widening the list.
            var targets = Enumerable.Range(1, 40).Select(i => new StationAiCameraMapTarget(new NetEntity(100000 + i),
                Vector2.Zero, $"Very long camera name for the engineering department number {i}", true, i % 3 != 0)).ToList();
            window.UpdateMap(EntityUid.Invalid, new StationAiCameraMapState(NetEntity.Invalid, targets, new(), TimeSpan.Zero));
        });
        // Start from the default landscape layout, as a player would before dragging the border.
        await RunTicks(5);
        await Client.WaitPost(() => window.SetSize = new Vector2(width, height));
        await RunTicks(5);
        await Client.WaitAssertion(() =>
        {
            Assert.That(TryGetControlFromChildren<StationAiCameraMapControl>(_ => true, window, out var map));
            Assert.That(TryGetControlFromChildren<ScrollContainer>(_ => true, window, out var list));
            TestContext.Out.WriteLine($"window {window.Size} map {map!.Size}@{map.Position} list {list!.Size}");
            Assert.That(window.Size.X, Is.EqualTo(width).Within(1), "Content must not hold the window wider");
            Assert.That(window.Size.Y, Is.EqualTo(height).Within(1), "Content must not hold the window taller");
            var listX = list.GlobalPosition.X - window.GlobalPosition.X;
            // FancyWindow grabs a 7px border for resizing; the map and the list scrollbar must leave it free.
            Assert.That(map.GlobalPosition.X - window.GlobalPosition.X, Is.GreaterThanOrEqualTo(7f), "Left resize edge");
            Assert.That(window.GlobalPosition.X + window.Size.X - (list.GlobalPosition.X + list.Size.X),
                Is.GreaterThanOrEqualTo(7f), "Right resize edge");
            if (portrait)
            {
                Assert.That(map.Width, Is.GreaterThan(width * 0.8f), "Map spans the window above the list");
                Assert.That(list.GlobalPosition.Y, Is.GreaterThan(map.GlobalPosition.Y + map.Height - 1));
            }
            else
            {
                Assert.That(listX, Is.GreaterThan(map.Width), "List sits to the right of the map");
                Assert.That(map.Height, Is.GreaterThan(height * 0.7f), "Map fills the window height");
            }

            window.Close();
        });
    }

    private static StationAiCameraMapState State(TimeSpan nextTeleport, int cameras = 40)
    {
        var targets = Enumerable.Range(1, cameras).Select(i => new StationAiCameraMapTarget(new NetEntity(100000 + i),
            Vector2.Zero, $"Camera {i}", true, i % 3 != 0)).ToList();
        return new StationAiCameraMapState(NetEntity.Invalid, targets, new(), nextTeleport);
    }

    [Test]
    public async Task SwitchEffectFiresOnlyWhenCooldownStarts()
    {
        StationAiCameraMapWindow window = default!;
        var switches = 0;
        await Client.WaitPost(() =>
        {
            window = new StationAiCameraMapWindow();
            window.OnCameraSwitched += () => switches++;
            window.OpenCentered();
            window.UpdateMap(EntityUid.Invalid, State(TimeSpan.FromSeconds(3)));
        });
        await Client.WaitAssertion(() =>
        {
            Assert.That(switches, Is.Zero, "Opening the window with an old cooldown is not a switch");
            window.UpdateMap(EntityUid.Invalid, State(TimeSpan.FromSeconds(3)));
            Assert.That(switches, Is.Zero, "Unchanged refresh");
            window.UpdateMap(EntityUid.Invalid, State(TimeSpan.FromSeconds(10)));
            Assert.That(switches, Is.EqualTo(1), "New cooldown means the view moved");
            window.Close();
        });
    }

    [Test]
    public async Task FilteringReusesCameraRows()
    {
        StationAiCameraMapWindow window = default!;
        await Client.WaitPost(() =>
        {
            window = new StationAiCameraMapWindow();
            window.OpenCentered();
            window.UpdateMap(EntityUid.Invalid, State(TimeSpan.Zero));
        });
        await RunTicks(2);
        await Client.WaitAssertion(() =>
        {
            Assert.That(TryGetControlFromChildren<ScrollContainer>(_ => true, window, out var scroll));
            var list = scroll!.GetChild(0);
            var rows = list.Children.OfType<ContainerButton>().ToList();
            Assert.That(rows, Has.Count.EqualTo(40));
            // Shuffled refresh order of the same cameras must not rebuild them.
            window.UpdateMap(EntityUid.Invalid, State(TimeSpan.Zero));
            Assert.That(list.Children.OfType<ContainerButton>(), Is.EquivalentTo(rows), "Unchanged cameras keep their row controls");
            window.UpdateMap(EntityUid.Invalid, State(TimeSpan.Zero, 39));
            Assert.That(rows.Count(r => r.Disposed), Is.EqualTo(1), "Only the removed camera's row is disposed");
            window.Close();
        });
    }

    [Test]
    public async Task WindowRestoresSavedBounds()
    {
        StationAiCameraMapWindow window = default!;
        await Client.WaitPost(() =>
        {
            window = new StationAiCameraMapWindow();
            window.OpenCentered();
            var preferences = new StationAiCameraMapPreferencesComponent
            {
                WindowSize = new Vector2(700, 600),
                WindowPosition = new Vector2(40, 30),
            };
            StationAiCameraMapBoundUserInterface.RestoreBounds(window, preferences);
        });
        await RunTicks(3);
        await Client.WaitAssertion(() =>
        {
            Assert.That(window.Size.X, Is.EqualTo(700).Within(1));
            Assert.That(window.Size.Y, Is.EqualTo(600).Within(1));
            Assert.That(window.Position.X, Is.EqualTo(40).Within(1));
            Assert.That(window.Position.Y, Is.EqualTo(30).Within(1));
            window.Close();
        });
    }

    [Test]
    public async Task MapShowsMarkersOfOpenTabOnly()
    {
        StationAiCameraMapWindow window = default!;
        var camera = new NetEntity(200001);
        var sensor = new NetEntity(200002);
        await Client.WaitPost(() =>
        {
            window = new StationAiCameraMapWindow();
            window.OpenCentered();
            var targets = new List<StationAiCameraMapTarget> { new(camera, Vector2.Zero, "Camera", true, true) };
            var crew = new List<StationAiCameraMapCrew> { new(sensor, "Crew", "Job", "JobIconPassenger", true, 0f, Vector2.One) };
            window.UpdateMap(EntityUid.Invalid, new StationAiCameraMapState(NetEntity.Invalid, targets, crew, TimeSpan.Zero));
        });
        await RunTicks(2);
        await Client.WaitAssertion(() =>
        {
            Assert.That(TryGetControlFromChildren<StationAiCameraMapControl>(_ => true, window, out var map));
            Assert.That(map!.TrackedEntities.Keys, Is.EquivalentTo(new[] { camera }), "Cameras tab");
        });
        await ClickControl(GetCrewTab(window));
        await Client.WaitAssertion(() =>
        {
            Assert.That(TryGetControlFromChildren<StationAiCameraMapControl>(_ => true, window, out var map));
            Assert.That(map!.TrackedEntities.Keys, Is.EquivalentTo(new[] { sensor }), "Crew tab");
            window.Close();
        });
    }

    // The crew tab is the only unpressed toggle of the tab group while the cameras tab is open.
    private Button GetCrewTab(StationAiCameraMapWindow window)
    {
        Assert.That(TryGetControlFromChildren<Button>(b => b.ToggleMode && b.Group != null && !b.Pressed, window, out var tab));
        return tab!;
    }
}
