// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameTicking.Managers;
using Content.Client.LateJoin;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.SS220.Hop;
using Content.IntegrationTests.Fixtures;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.SS220.Hop;

public sealed class LateJoinPriorityTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: playTimeTracker
  id: HopLateJoinPriorityTestTracker

- type: job
  id: HopLateJoinPriorityTestRole
  name: job-name-passenger
  playTimeTracker: HopLateJoinPriorityTestTracker
  experienceDefinition: NoExperience
  setPreference: false
""";

    [Test]
    public async Task PriorityRecruitmentAppearsBeforeDepartmentsAndTracksChanges()
    {
        await Client.WaitAssertion(() =>
        {
            var ticker = Client.System<ClientGameTicker>();
            var station = new NetEntity(123456);
            var previous = new TickerJobsAvailableEvent(ticker.StationNames.ToDictionary(x => x.Key, x => x.Value),
                ticker.JobsAvailable.ToDictionary(x => x.Key, x => new Dictionary<ProtoId<JobPrototype>, int?>(x.Value)),
                ticker.PriorityJobs.ToDictionary(x => x.Key, x => new HashSet<ProtoId<JobPrototype>>(x.Value)));
            LateJoinGui window = null;
            try
            {
                SendJobs(station, "Passenger", 2);
                window = new LateJoinGui();
                var section = Descendants(window).OfType<PriorityJobsSection>().Single();
                Assert.That(section.Visible, Is.True);
                Assert.That(section.Parent.GetChild(0), Is.SameAs(section), "Priority recruitment precedes all departments");
                var priority = (ContainerButton) section.Jobs.GetChild(0);
                Assert.That(priority.Name, Is.EqualTo("Passenger"));
                Assert.That(Descendants(window).OfType<ContainerButton>().Count(b => b.Name == "Passenger"), Is.GreaterThan(1),
                    "The department listing is retained");
                AssertMatchingAvailability(window, "Passenger");

                SendJobs(station, "Passenger", 1);
                Assert.That(section.Jobs.GetChild(0), Is.SameAs(priority), "Slot updates do not replace a clickable control");
                Assert.That(Descendants(priority).OfType<Label>().Any(l => l.Text.Contains("1")), Is.True);

                SendJobs(station, "Captain", 1);
                Assert.That(section.Jobs.ChildCount, Is.EqualTo(1));
                Assert.That(((ContainerButton) section.Jobs.GetChild(0)).Name, Is.EqualTo("Captain"));
                AssertMatchingAvailability(window, "Captain");

                SendJobs(station, "Captain", 0);
                Assert.That(section.Visible, Is.False, "No recruitment banner for filled positions");
                SendJobs(station, "Captain", 1);
                Assert.That(section.Visible, Is.True);
                AssertMatchingAvailability(window, "Captain");
                SendJobs(station, null, 1);
                Assert.That(section.Visible, Is.False, "Removing priority hides the banner immediately");
                Assert.That(section.Jobs.ChildCount, Is.EqualTo(0));

                SendJobs(station, "HopLateJoinPriorityTestRole", null);
                Assert.That(section.Visible, Is.True, "Priority roles do not need a department to be displayed");
                Assert.That(((ContainerButton) section.Jobs.GetChild(0)).Name, Is.EqualTo("HopLateJoinPriorityTestRole"));
                Assert.That(Descendants(window).OfType<ContainerButton>().Count(b => b.Name == "HopLateJoinPriorityTestRole"),
                    Is.EqualTo(1), "The department-independent role appears only in the priority block");
            }
            finally
            {
                window?.Dispose();
                CEntMan.EventBus.RaiseEvent(EventSource.Network, previous);
            }
        });
    }

    private void SendJobs(NetEntity station, string priority, int? slots)
    {
        var jobs = new Dictionary<ProtoId<JobPrototype>, int?> { ["Passenger"] = 2, ["Captain"] = 1 };
        var priorities = new HashSet<ProtoId<JobPrototype>>();
        if (priority != null)
        {
            jobs[priority] = slots;
            priorities.Add(priority);
        }
        CEntMan.EventBus.RaiseEvent(EventSource.Network, new TickerJobsAvailableEvent(
            new() { [station] = "Recruitment test station" }, new() { [station] = jobs }, new() { [station] = priorities }));
    }

    private void AssertMatchingAvailability(LateJoinGui window, string id)
    {
        var buttons = Descendants(window).OfType<ContainerButton>().Where(b => b.Name == id).ToArray();
        var job = CProtoMan.Index<JobPrototype>(id);
        var profile = (HumanoidCharacterProfile) Client.ResolveDependency<IClientPreferencesManager>().Preferences?.SelectedCharacter;
        var allowed = Client.ResolveDependency<JobRequirementsManager>().IsAllowed(job, profile, out _);
        Assert.That(buttons.Length, Is.GreaterThan(1));
        Assert.That(buttons.All(b => b.Disabled == !allowed), Is.True,
            "Priority and department buttons enforce the same role requirements");
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
