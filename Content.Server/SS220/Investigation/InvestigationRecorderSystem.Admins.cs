// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.SS220.Investigation;

public sealed partial class InvestigationRecorderSystem
{
    private readonly HashSet<Guid> _presentAdmins = new();

    private void InitializeAdmins()
    {
        _admin.OnPermsChanged += OnAdminPermsChanged;
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void ShutdownAdmins()
    {
        _admin.OnPermsChanged -= OnAdminPermsChanged;
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        SetAdminPresence(args.Player, args.Flags is not null);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            SetAdminPresence(args.Session, false);
    }

    private void SnapshotAdmins()
    {
        _presentAdmins.Clear();

        foreach (var session in _admin.ActiveAdmins)
        {
            _presentAdmins.Add(session.UserId.UserId);
            _recorder.WriteAdminPresence(session.UserId.UserId, session.Name, true);
        }
    }

    private void SetAdminPresence(ICommonSession session, bool active)
    {
        if (!_recorder.IsRecording)
            return;

        var player = session.UserId.UserId;

        // OnPermsChanged also fires for flag edits that leave an admin adminned, which must not emit a row.
        if (active ? !_presentAdmins.Add(player) : !_presentAdmins.Remove(player))
            return;

        _recorder.WriteAdminPresence(player, session.Name, active);
    }
}
