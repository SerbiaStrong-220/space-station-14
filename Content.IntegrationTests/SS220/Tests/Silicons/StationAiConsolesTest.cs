// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

#nullable enable
using System.Collections.Generic;
using System.Numerics;
using Content.Client.SS220.SuperMatter.Observer;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Tests.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Materials;
using Content.Shared.Atmos;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Power;
using Content.Shared.Robotics.Components;
using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Silicons;

/// <summary>
/// Station AI opens station consoles remotely, so they must keep working wherever its eye is.
/// </summary>
[TestFixture]
public sealed class StationAiConsolesTest : GameTest
{
    private static readonly EntProtoId CoreProto = "PlayerStationAiEmpty";
    private static readonly EntProtoId BrainProto = "StationAiBrain";
    private static readonly EntProtoId BorgProto = "BorgChassisGeneric";
    private static readonly EntProtoId CableProto = "CableHV";
    private const string MindSlot = "station_ai_mind_slot";

    private const uint AdditionalFrequency = 2201;

    [TestPrototypes]
    private const string Prototypes = @"
- type: deviceFrequency
  id: SS220TestAdditionalFrequency
  frequency: 2201

- type: entity
  id: SS220TestAdditionalFrequencyReceiver
  components:
  - type: DeviceNetwork
    receiveFrequency: 2200
    transmitFrequency: 2200
    additionalReceiveFrequencyIds:
    - SS220TestAdditionalFrequency

- type: entity
  id: SS220TestAdditionalFrequencySender
  components:
  - type: DeviceNetwork
    receiveFrequency: 2202
    transmitFrequency: 2201
";

    [Test]
    public async Task AdditionalReceiveFrequencyTest()
    {
        var deviceNet = Server.System<DeviceNetworkSystem>();
        var testSystem = Server.System<DeviceNetworkTestSystem>();

        EntityUid receiver = default;
        EntityUid sender = default;
        await Server.WaitPost(() =>
        {
            receiver = SEntMan.SpawnEntity("SS220TestAdditionalFrequencyReceiver", MapCoordinates.Nullspace);
            sender = SEntMan.SpawnEntity("SS220TestAdditionalFrequencySender", MapCoordinates.Nullspace);
        });

        var receiverDevice = SComp<DeviceNetworkComponent>(receiver);
        Assert.That(receiverDevice.AdditionalReceiveFrequencies, Does.Contain(AdditionalFrequency));

        var broadcast = new NetworkPayload { ["Test"] = "broadcast" };
        await SendAndRun(() => deviceNet.QueuePacket(sender, null, broadcast));
        Assert.That(testSystem.LastPayload, Is.SameAs(broadcast),
            "Broadcast on an additional frequency was not received.");

        var targeted = new NetworkPayload { ["Test"] = "targeted" };
        await SendAndRun(() => deviceNet.QueuePacket(sender, receiverDevice.Address, targeted));
        Assert.That(testSystem.LastPayload, Is.SameAs(targeted),
            "Targeted packet on an additional frequency was not received.");

        var foreign = new NetworkPayload { ["Test"] = "foreign" };
        await SendAndRun(() => deviceNet.QueuePacket(sender, receiverDevice.Address, foreign, 2203));
        Assert.That(testSystem.LastPayload, Is.Not.SameAs(foreign),
            "Packet on a frequency the device does not listen to was received.");

        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(receiver);
            SEntMan.DeleteEntity(sender);
        });

        async Task SendAndRun(Action send)
        {
            await Server.WaitPost(() =>
            {
                testSystem.LastPayload = default!;
                send();
            });
            await Server.WaitRunTicks(2);
        }
    }

    [Test]
    public async Task RoboticsConsoleReceivesCyborgsTest()
    {
        var map = await Pair.CreateTestMap();
        var (_, ai) = await SpawnStationAi(map.GridCoords);

        EntityUid borg = default;
        await Server.WaitPost(() => borg = SEntMan.SpawnEntity(BorgProto, map.GridCoords));
        await RunSeconds(2);

        var borgDevice = SComp<DeviceNetworkComponent>(borg);
        var console = SComp<RoboticsConsoleComponent>(ai);

        Assert.That(console.Cyborgs.Keys, Does.Contain(borgDevice.Address),
            "Station AI robotics console did not receive the cyborg broadcast.");
        Assert.That(console.CommandFrequency, Is.Not.Null);
        Assert.That(SProtoMan.Index(console.CommandFrequency!.Value).Frequency, Is.EqualTo(borgDevice.ReceiveFrequency),
            "Station AI sends disable/destroy commands on a frequency cyborgs do not listen to.");
    }

    [Test]
    public async Task PowerMonitoringShowsCablesTest()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitPost(() => SEntMan.SpawnEntity(CableProto, map.GridCoords));
        var (_, ai) = await SpawnStationAi(map.GridCoords);
        await Server.WaitRunTicks(2);

        Assert.That(STryComp<PowerMonitoringCableNetworksComponent>(ai, out var cables), Is.True,
            "Station AI has no cable networks for its power monitoring console.");
        Assert.That(cables!.AllChunks, Is.Not.Empty,
            "Station AI power monitoring console has no cable chunks.");
    }

    [Test]
    public async Task SuperMatterObserverWorksAwayFromConsolesTest()
    {
        var map = await Pair.CreateTestMap();
        var (_, ai) = await SpawnStationAi(map.GridCoords);

        await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, ai));
        await RunUntilSynced();

        const int crystalId = 2200;
        var gridNet = SEntMan.GetNetEntity(map.Grid.Owner);
        await Server.WaitPost(() =>
        {
            var update = new SuperMatterStateUpdate(crystalId, gridNet, true, "test crystal",
                100f, 100f, 300f, (1f, 0f), (1f, 0f),
                new Dictionary<Gas, float>(), 0f, (false, TimeSpan.Zero));
            SEntMan.EntityNetManager!.SendSystemNetworkMessage(update);
        });
        await RunTicksSync(5);

        var clientAi = ToClientUid(ai);
        Assert.That(CEntMan.TryGetComponent<SuperMatterObserverComponent>(clientAi, out var observer), Is.True);
        Assert.That(observer!.Names.ContainsKey(crystalId), Is.True,
            "Station AI built-in observer ignored the crystal update because the AI is inside its core.");

        await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, null));
    }

    [Test]
    public async Task OreSiloPreloadUsesAiEyeTest()
    {
        var map = await Pair.CreateTestMap();
        var (core, ai) = await SpawnStationAi(map.GridCoords);
        var oreSilo = Server.System<OreSiloSystem>();

        EntityUid eye = default;
        await Server.WaitPost(() =>
        {
            // The test grid has a single tile, keep the eye on it but away from the core
            eye = SEntMan.SpawnEntity(null, map.GridCoords.Offset(new Vector2(0.3f, 0.2f)));
            SEntMan.System<SharedEyeSystem>().SetTarget(ai, eye, SEntMan.EnsureComponent<EyeComponent>(ai));
        });

        await Server.WaitAssertion(() =>
        {
            var (grid, position) = oreSilo.GetViewerGridPosition(ai, SComp<TransformComponent>(ai));
            Assert.That(grid, Is.EqualTo(map.Grid.Owner));
            Assert.That(Vector2.Distance(position, SComp<TransformComponent>(eye).LocalPosition), Is.LessThan(0.01f),
                "Ore silo preload does not use the position of the AI eye.");

            SEntMan.System<SharedEyeSystem>().SetTarget(ai, null);
            (grid, position) = oreSilo.GetViewerGridPosition(ai, SComp<TransformComponent>(ai));
            Assert.That(grid, Is.EqualTo(map.Grid.Owner));
            Assert.That(Vector2.Distance(position, SComp<TransformComponent>(core).LocalPosition), Is.LessThan(0.01f),
                "Ore silo preload uses the position inside the AI core instead of the core on the grid.");
        });
    }

    private async Task<(EntityUid Core, EntityUid Ai)> SpawnStationAi(EntityCoordinates coordinates)
    {
        EntityUid core = default;
        EntityUid ai = default;

        await Server.WaitAssertion(() =>
        {
            var containers = SEntMan.System<SharedContainerSystem>();
            core = SEntMan.SpawnEntity(CoreProto, coordinates);
            ai = SEntMan.SpawnEntity(BrainProto, coordinates);

            Assert.That(containers.Insert(ai, containers.GetContainer(core, MindSlot)), Is.True);
        });

        return (core, ai);
    }
}
