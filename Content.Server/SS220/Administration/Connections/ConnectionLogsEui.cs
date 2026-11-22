// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.SS220.Administration.Connections;
using static Content.Shared.SS220.Administration.Connections.ConnectionLogsEuiMsg;

namespace Content.Server.SS220.Administration.Connections;

public sealed class ConnectionLogsEui : BaseEui
{
    private const int PageSize = 50;
    // A regex input/complexity bound, not the maximum length of an account name.
    private const int MaxSearchPatternLength = 128;
    private const int RegexTimeoutMilliseconds = 250;
    private const string SawmillName = "admin.connections";

    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private readonly CancellationTokenSource _cancel = new();
    private string _ckey;
    private List<ConnectionLogInfo> _connectionLogs = [];
    private bool _isLoading;
    private bool _hasNext;
    private string? _error;

    public ConnectionLogsEui() : this(string.Empty)
    {
    }

    public ConnectionLogsEui(string ckey)
    {
        IoCManager.InjectDependencies(this);
        _ckey = ckey;
    }

    public override void Opened()
    {
        _admins.OnPermsChanged += OnPermsChanged;
        if (_ckey.Length > 0)
            LoadPage();
        else
            StateDirty();
    }

    public override void Closed()
    {
        _admins.OnPermsChanged -= OnPermsChanged;
        _cancel.Cancel();
        _cancel.Dispose();
    }

    public override EuiStateBase GetNewState()
    {
        return new ConnectionLogsEuiState(_ckey, _connectionLogs, _isLoading, _hasNext, _error);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!CanView() || _isLoading)
            return;

        switch (msg)
        {
            case Search search:
                _ckey = search.Ckey.Trim();
                _connectionLogs = [];
                _hasNext = false;
                if (_ckey.Length is 0 or > MaxSearchPatternLength)
                {
                    _error = "connection-logs-invalid-ckey";
                    StateDirty();
                    return;
                }

                LoadPage();
                break;
            case NextPage when _hasNext:
                LoadPage();
                break;
        }
    }

    private bool CanView()
    {
        if (IsShutDown)
            return false;

        if (_admins.HasAdminFlag(Player, AdminFlags.Logs))
            return true;

        Close();
        return false;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player)
            CanView();
    }

    private async void LoadPage()
    {
        if (!CanView())
            return;

        try
        {
            _ = new Regex(_ckey, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(RegexTimeoutMilliseconds));
        }
        catch (ArgumentException)
        {
            _error = "connection-logs-invalid-regex";
            StateDirty();
            return;
        }

        _isLoading = true;
        _error = null;
        StateDirty();
        var cancel = _cancel.Token;

        try
        {
            var last = _connectionLogs.LastOrDefault();
            var page = await _db.GetConnectionLogsAsync(_ckey, last?.Time, last?.Id, PageSize + 1, cancel);
            // Permissions may have changed while the database/auth request was in flight.
            if (!CanView())
                return;

            if (last == null)
                _adminLog.Add(LogType.Action, $"{Player:actor} searched connection logs with regex {_ckey}");

            _hasNext = page.Count > PageSize;
            _connectionLogs.AddRange(page.Take(PageSize).Select(log => new ConnectionLogInfo(
                log.Id,
                log.UserName,
                log.Time,
                log.Address,
                log.Denied?.ToString(),
                log.ServerName,
                log.Trust)));
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _log.GetSawmill(SawmillName).Error($"Failed to load connection logs: {exception}");
            _error = "connection-logs-error";
        }
        finally
        {
            _isLoading = false;
            if (CanView())
                StateDirty();
        }
    }
}
