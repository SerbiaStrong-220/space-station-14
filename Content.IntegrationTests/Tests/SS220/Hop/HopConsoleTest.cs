// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.Client.SS220.Hop;
using Content.IntegrationTests.Fixtures;
using Content.Server.SS220.Hop;
using Content.Server.Station.Systems;
using Content.Server.GameTicking;
using Robust.Shared.Network;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Maps;
using Content.Shared.SS220.Hop;
using Content.Shared.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.SS220.Hop;

public sealed class HopConsoleTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: gameMap
  id: HopConsoleTestMap
  minPlayers: 0
  mapName: Hop test
  mapPath: /Maps/Test/empty.yml
  stations:
    Station:
      stationProto: TestStation
      components:
      - type: StationEventEligible
      - type: StationJobs
        availableJobs:
          Captain: [1, 1]
          HeadOfPersonnel: [1, 1]
          StationEngineer: [4, 4]
          Passenger: [-1, -1]
          CBURN: [-1, -1]
          CentComNanoTrasenAdmiral: [-1, -1]
""";

    [Test]
    public async Task RealStationLimitsAndSpecialRoles()
    {
        await Server.WaitAssertion(() =>
        {
            var proto = Server.ResolveDependency<IPrototypeManager>().Index<GameMapPrototype>("HopConsoleTestMap");
            var station = Server.System<StationSystem>().InitializeNewStation(proto.Stations["Station"], null, "HoP test");
            var system = Server.System<HopConsoleSystem>();
            var state = system.BuildState(station);
            Assert.That(state.Available, Is.True);
            Assert.That(state.Jobs.Select(j => j.Id), Is.EquivalentTo(new[] { "Captain", "HeadOfPersonnel", "Passenger", "StationEngineer" }));
            foreach (var id in new[] { "Captain", "HeadOfPersonnel" })
            {
                var job = state.Jobs.Single(j => j.Id == id);
                Assert.That(job.RoundStartSlots, Is.EqualTo(1));
                Assert.That(job.ConfiguredSlots, Is.EqualTo(1));
                Assert.That(job.AvailableSlots, Is.EqualTo(1));
            }
            Assert.That(state.Jobs.Single(j => j.Id == "Passenger").AvailableSlots, Is.Null);
            Server.System<StationJobsSystem>().TrySetJobSlot(station, "Captain", 0);
            Assert.That(system.BuildState(station).Jobs.Single(j => j.Id == "Captain").AvailableSlots, Is.EqualTo(0));
            Server.System<StationJobsSystem>().TrySetJobSlot(station, "Captain", 1);
            Assert.That(system.BuildState(station).Jobs.Single(j => j.Id == "Captain").AvailableSlots, Is.EqualTo(1));
            SEntMan.DeleteEntity(station);
        });
    }

    [Test]
    public async Task ConsoleChecksAccessAndRejectsStaleOrSpecialJobWrites()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var proto = Server.ResolveDependency<IPrototypeManager>().Index<GameMapPrototype>("HopConsoleTestMap");
            var stations = Server.System<StationSystem>();
            var station = stations.InitializeNewStation(proto.Stations["Station"], null, "HoP test");
            stations.AddGridToStation(station, map.GridCoords.EntityId);
            var console = SEntMan.SpawnEntity("ComputerCrewRecordsManagement", map.GridCoords);
            var hop = SEntMan.SpawnEntity("HoPIDCard", map.GridCoords);
            var passenger = SEntMan.SpawnEntity("PassengerIDCard", map.GridCoords);
            var system = Server.System<HopConsoleSystem>();
            Assert.That(Server.System<AccessReaderSystem>().IsAllowed(hop, console), Is.True,
                "The spawned HoP credential must grant console access");
            Assert.That(Server.System<AccessReaderSystem>().IsAllowed(passenger, console), Is.False);
            Assert.That(SEntMan.HasComponent<HopConsoleComponent>(console), Is.True);
            Assert.That(SEntMan.HasComponent<IdCardConsoleComponent>(console), Is.False);
            Assert.That(SEntMan.GetComponent<ActivatableUIComponent>(console).Key, Is.EqualTo(HopConsoleUiKey.Key));

            void Send(BoundUserInterfaceMessage message, EntityUid actor)
            {
                message.Actor = actor;
                message.UiKey = HopConsoleUiKey.Key;
                // Dispatch by the concrete network-message type, not the helper's base class.
                SEntMan.EventBus.RaiseLocalEvent(console, (object) message);
            }

            Send(new HopConsoleSaveJobMessage("Captain", 7, true, 1, false), passenger);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "Captain").AvailableSlots, Is.EqualTo(1));
            Send(new HopConsoleSaveJobMessage("Captain", 2, true, 1, false), hop);
            var captain = system.BuildState(console).Jobs.Single(j => j.Id == "Captain");
            Assert.That(captain.AvailableSlots, Is.EqualTo(1), "Base 1 permits no extra slot after rounding down");
            Assert.That(captain.Priority, Is.False, "Rejected saves must be atomic");
            Send(new HopConsoleSaveJobMessage("Captain", null, true, 1, false), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "Captain").AvailableSlots, Is.EqualTo(1));
            Send(new HopConsoleSaveJobMessage("Captain", 1, true, 1, false), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "Captain").Priority, Is.True);
            Send(new HopConsoleSaveJobMessage("Captain", 9, false, 1, false), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "Captain").AvailableSlots, Is.EqualTo(1));
            Send(new HopConsoleSaveJobMessage("HeadOfPersonnel", 1, true, 1, false), hop);
            Assert.That(system.BuildState(console).Jobs.Where(j => j.Priority).Select(j => j.Id),
                Is.EquivalentTo(new[] { "HeadOfPersonnel" }));
            Send(new HopConsoleSaveJobMessage("Passenger", 5, false, null, false), hop);
            Send(new HopConsoleSaveJobMessage("Passenger", null, false, 5, false), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "Passenger").AvailableSlots, Is.Null);

            // Increasing and closing slots repeatedly must not accumulate another 50% allowance.
            Send(new HopConsoleSaveJobMessage("StationEngineer", 7, false, 4, false), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "StationEngineer").AvailableSlots, Is.EqualTo(4));
            Send(new HopConsoleSaveJobMessage("StationEngineer", 6, false, 4, false), hop);
            var jobs = Server.System<StationJobsSystem>();
            Assert.That(jobs.TryAssignJob(station, "StationEngineer", new NetUserId(Guid.NewGuid())), Is.True);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "StationEngineer").MaximumAvailable, Is.EqualTo(5));
            Send(new HopConsoleSaveJobMessage("StationEngineer", 6, false, 5, false), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "StationEngineer").AvailableSlots, Is.EqualTo(5));
            Send(new HopConsoleSaveJobMessage("StationEngineer", 0, false, 5, false), hop);
            Send(new HopConsoleSaveJobMessage("StationEngineer", 5, false, 0, false), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "StationEngineer").AvailableSlots, Is.EqualTo(5));
            Send(new HopConsoleSaveJobMessage("CBURN", 5, true, null, false), hop);
            Server.System<StationJobsSystem>().TryGetJobSlot(station, "CBURN", out var cburn);
            Assert.That(cburn, Is.Null);

            Send(new HopConsoleAddRecordMessage("Test Employee", "CBURN", ""), hop);
            Assert.That(system.BuildState(console).Records, Is.Empty);
            Send(new HopConsoleAddRecordMessage("Test Employee", "Captain", ""), hop);
            var record = system.BuildState(console).Records.Single();
            Assert.That(record.JobId, Is.EqualTo("Captain"));
            Send(new HopConsoleAddRecordMessage("Test Employee", "Captain", ""), hop);
            Assert.That(system.BuildState(console).Records, Has.Count.EqualTo(1));
            Send(new HopConsoleRemoveRecordMessage(record.Key), passenger);
            Assert.That(system.BuildState(console).Records, Has.Count.EqualTo(1));
            Send(new HopConsoleRemoveRecordMessage(record.Key), hop);
            Assert.That(system.BuildState(console).Records, Is.Empty);

            // The real event runs both its randomized branches through the restriction hook.
            var ticker = Server.System<GameTicker>();
            for (var i = 0; i < 40; i++)
            {
                var before = jobs.GetJobs(station).ToDictionary(x => x.Key, x => x.Value);
                ticker.StartGameRule("BureaucraticError", out var rule);
                // An event may have an announcement delay; a second start skips that delay.
                ticker.StartGameRule(rule);
                var after = jobs.GetJobs(station);
                var changed = after.Where(x => before[x.Key] != x.Value).Select(x => x.Key).ToList();
                if (changed.Count > 0)
                {
                    var restrictions = SEntMan.GetComponent<HopStationRestrictionsComponent>(station);
                    Assert.That(changed.All(id => restrictions.LockedJobs.Contains(id)), Is.True);
                }
                SEntMan.DeleteEntity(rule);
            }
            var locked = system.BuildState(console).Jobs.Where(j => j.SlotsLocked).ToList();
            Assert.That(locked, Is.Not.Empty);
            Assert.That(system.BuildState(console).Jobs.Count(j => j.Priority), Is.EqualTo(1));
            foreach (var jobEntry in locked)
            {
                Send(new HopConsoleSaveJobMessage(jobEntry.Id, jobEntry.AvailableSlots == 0 ? 1 : 0,
                    jobEntry.Priority, jobEntry.AvailableSlots, jobEntry.Priority), hop);
                Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == jobEntry.Id).AvailableSlots,
                    Is.EqualTo(jobEntry.AvailableSlots), "Event limits must be immutable");
            }
            var forcedPriority = system.BuildState(console).Jobs.Single(j => j.Priority);
            Send(new HopConsoleSaveJobMessage(forcedPriority.Id, forcedPriority.AvailableSlots,
                false, forcedPriority.AvailableSlots, true), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Priority).Id, Is.EqualTo(forcedPriority.Id));

            // Explicitly cover a second event affecting several roles while a forced priority already exists.
            jobs.MakeJobUnlimited(station, "StationEngineer");
            SEntMan.EventBus.RaiseEvent(EventSource.Local, new BureaucraticJobsChangedEvent(station,
                ["StationEngineer", "Captain"], "StationEngineer"));
            var eventState = system.BuildState(console);
            Assert.That(eventState.Jobs.Where(j => j.Priority).Select(j => j.Id), Is.EquivalentTo(new[] { "StationEngineer" }));
            Assert.That(eventState.Jobs.Single(j => j.Id == "StationEngineer").SlotsLocked, Is.True);
            Send(new HopConsoleSaveJobMessage("StationEngineer", 6, true, null, true), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Id == "StationEngineer").AvailableSlots, Is.Null);
            var other = system.BuildState(console).Jobs.First(j => !j.Priority);
            Send(new HopConsoleSaveJobMessage(other.Id, other.AvailableSlots, true, other.AvailableSlots, false), hop);
            Assert.That(system.BuildState(console).Jobs.Single(j => j.Priority).Id, Is.EqualTo("StationEngineer"));

            SEntMan.DeleteEntity(console);
            SEntMan.DeleteEntity(hop);
            SEntMan.DeleteEntity(passenger);
            stations.RemoveGridFromStation(station, map.GridCoords.EntityId);
            SEntMan.DeleteEntity(station);
        });
    }

    [Test]
    public async Task WindowLoadsAndKeepsUnsavedInputOnRefresh()
    {
        await Client.WaitAssertion(() =>
        {
            // No requests are sent in this test; exercise the real XAML and input/state handlers.
            var window = new HopConsoleWindow(null!);
            var state = new HopConsoleState("Test station",
                [new("Captain", "Captain", 1, false, 1, 1), new("HeadOfPersonnel", "Head of Personnel", 1, false, 1, 1)],
                [new(1, "Test person", "Captain", "Captain")]);
            window.UpdateState(state);
            Assert.That(window.FindControl<CheckBox>("Unlimited").Pressed, Is.False);
            Assert.That(window.FindControl<CheckBox>("Unlimited").Visible, Is.False);
            var slots = window.FindControl<LineEdit>("Slots");
            var save = window.FindControl<Button>("SaveJob");
            slots.SetText("invalid", invokeEvent: true);
            Assert.That(save.Disabled, Is.True);
            slots.SetText("2", invokeEvent: true);
            Assert.That(save.Disabled, Is.True);
            slots.SetText("0", invokeEvent: true);
            Assert.That(save.Disabled, Is.False);
            var name = window.FindControl<LineEdit>("NewName");
            name.SetText("Draft name", invokeEvent: true);
            var job = window.FindControl<OptionButton>("NewJob");
            job.SelectId(1);
            var firstButton = window.FindControl<BoxContainer>("JobList").GetChild(0);
            window.UpdateState(state);
            Assert.That(slots.Text, Is.EqualTo("0"));
            Assert.That(name.Text, Is.EqualTo("Draft name"));
            Assert.That(job.SelectedId, Is.EqualTo(1));
            Assert.That(window.FindControl<BoxContainer>("JobList").GetChild(0), Is.SameAs(firstButton),
                "Unchanged server updates must not rebuild the list or interrupt a click");
            window.FindControl<LineEdit>("JobSearch").SetText("Captain", invokeEvent: true);
            Assert.That(window.FindControl<BoxContainer>("JobList").ChildCount, Is.EqualTo(1));
            window.Measure(new Vector2(940, 660));
            Assert.That(window.DesiredSize.X, Is.LessThanOrEqualTo(940));
            Assert.That(window.DesiredSize.Y, Is.LessThanOrEqualTo(660));
            window.FindControl<TabContainer>("Tabs").CurrentTab = 1;
            window.FindControl<BoxContainer>("RecordForm").Visible = true;
            window.FindControl<Label>("RecordEmpty").Visible = false;
            window.Measure(new Vector2(940, 660));
            Assert.That(window.DesiredSize.X, Is.LessThanOrEqualTo(940), "Record form width");
            Assert.That(window.DesiredSize.Y, Is.LessThanOrEqualTo(660), "Record form height");
            window.FindControl<BoxContainer>("RecordForm").Visible = false;
            window.FindControl<BoxContainer>("RecordDetails").Visible = true;
            window.FindControl<BoxContainer>("DeleteConfirmation").Visible = true;
            window.Measure(new Vector2(940, 660));
            Assert.That(window.DesiredSize.X, Is.LessThanOrEqualTo(940), "Delete confirmation width");
            Assert.That(window.DesiredSize.Y, Is.LessThanOrEqualTo(660), "Delete confirmation height");

            window.UpdateState(new HopConsoleState("Test", [new("Passenger", "Passenger", null, false, null, null)], []));
            Assert.That(window.FindControl<CheckBox>("Unlimited").Visible, Is.True);
            window.UpdateState(new HopConsoleState("Test",
                [new("StationEngineer", "Engineer", null, true, 4, 4, slotsLocked: true, priorityLocked: true)], []));
            Assert.That(window.FindControl<CheckBox>("Unlimited").Visible, Is.False);
            Assert.That(window.FindControl<LineEdit>("Slots").Editable, Is.False);
            Assert.That(window.FindControl<CheckBox>("Priority").Disabled, Is.True);
            Assert.That(window.FindControl<Button>("IncreaseSlots").Disabled, Is.True);
        });
    }
}
