// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Hop;

namespace Content.Client.SS220.Hop;

public sealed class HopConsoleBoundUserInterface : BoundUserInterface
{
    private HopConsoleWindow? _window;

    public HopConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new HopConsoleWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();
        if (State is HopConsoleState state)
            _window.UpdateState(state);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is HopConsoleState hopState)
            _window?.UpdateState(hopState);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is HopConsoleResultMessage result)
        {
            _window?.UpdateState(result.State);
            _window?.HandleResult(result);
        }
    }

    public void SaveJob(HopJobEntry job, int? slots, bool priority)
    {
        SendMessage(new HopConsoleSaveJobMessage(job.Id, slots, priority, job.AvailableSlots, job.Priority));
    }

    public void Refresh() => SendMessage(new HopConsoleRefreshMessage());

    public void AddRecord(string name, string jobId, string title)
    {
        SendMessage(new HopConsoleAddRecordMessage(name, jobId, title));
    }

    public void RemoveRecord(uint key)
    {
        SendMessage(new HopConsoleRemoveRecordMessage(key));
    }
}
