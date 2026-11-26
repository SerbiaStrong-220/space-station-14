using System.Linq;
using Content.Client.Administration.UI;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Administration.UI.Logs;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.IntegrationTests.Tests.Administration.Logs;

public sealed class LogWindowTest : InteractionTest
{
    protected override PoolSettings Settings => new() { Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false };

    [Test]
    public async Task TestAdminLogsWindow()
    {
        // First, generate a new log
        var log = Server.Resolve<IAdminLogManager>();
        var guid = Guid.NewGuid();
        await Server.WaitPost(() => log.Add(LogType.Unknown, $"{SPlayer} test log 1: {guid}"));

        // Click the admin button in the menu bar
        await ClickWidgetControl<GameTopMenuBar, MenuButton>(nameof(GameTopMenuBar.AdminButton));
        var adminWindow = GetWindow<AdminMenuWindow>();

        // Find and click the "open logs" button.
        Assert.That(TryGetControlFromChildren<CommandButton>(x => x.Command == OpenAdminLogsCommand.Cmd, adminWindow, out var btn));
        await ClickControl(btn!);
        var logWindow = GetWindow<AdminLogsWindow>();

        // Find the log search field and refresh buttons
        var search = logWindow.Logs.LogSearch;
        var refresh = logWindow.Logs.RefreshButton;
        // SS220 check fix begin
        // Wait for the initial round state and automatic query before refreshing.
        await WaitForLog(logWindow, guid);
        // SS220 check fix end

        // Search for the log we added earlier.
        await Client.WaitPost(() => search.Text = guid.ToString());
        await ClickControl(refresh);
        // SS220 check fix begin
        var searchResult = await WaitForLog(logWindow, guid);
        Assert.That(searchResult.Length, Is.EqualTo(1));
        Assert.That(searchResult[0].Message, Contains.Substring($" test log 1: {guid}"));
        // SS220 check fix end

        // Add a new log
        guid = Guid.NewGuid();
        await Server.WaitPost(() => log.Add(LogType.Unknown, $"{SPlayer} test log 2: {guid}"));

        // Update the search and refresh
        await Client.WaitPost(() => search.Text = guid.ToString());
        await ClickControl(refresh);
        // SS220 check fix begin
        searchResult = await WaitForLog(logWindow, guid);
        Assert.That(searchResult.Length, Is.EqualTo(1));
        Assert.That(searchResult[0].Message, Contains.Substring($" test log 2: {guid}"));
        // SS220 check fix end
    }

    // SS220 check fix begin
    private async Task<SharedAdminLog[]> WaitForLog(AdminLogsWindow window, Guid guid)
    {
        const int maxTicks = 600;
        var expected = guid.ToString();
        SharedAdminLog[] visible = [];
        SharedAdminLog[] all = [];
        var roundId = 0;

        for (var tick = 0; tick <= maxTicks; tick++)
        {
            await Client.WaitPost(() =>
            {
                var labels = window.Logs.LogsContainer.Children.OfType<AdminLogLabel>().ToArray();
                all = labels.Select(label => label.Log).ToArray();
                visible = labels.Where(label => label.Visible).Select(label => label.Log).ToArray();
                roundId = window.Logs.SelectedRoundId;
            });

            if (visible.Any(log => log.Message.Contains(expected)))
                return visible;

            if (tick < maxTicks)
                await RunTicks(1);
        }

        Assert.Fail($"Log {expected} did not appear after {maxTicks} ticks. " +
                    $"Round: {roundId}; total labels: {all.Length}; visible: {visible.Length}; " +
                    $"present before visibility filtering: {all.Any(log => log.Message.Contains(expected))}.");
        return visible;
    }
    // SS220 check fix end
}
