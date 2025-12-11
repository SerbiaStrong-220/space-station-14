// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.Json;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.SS220.Signature;

namespace Content.Server.SS220.Signature;

public sealed class SignatureSystem : SharedSignatureSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestSignatureAdminMessage>(OnRequestSignatureAdmin);
    }

    private async void OnRequestSignatureAdmin(RequestSignatureAdminMessage args, EntitySessionEventArgs ev)
    {
        var userEnt = ev.SenderSession.AttachedEntity;
        if (userEnt == null)
            return;

        if (!_adminManager.IsAdmin(ev.SenderSession, true))
        {
            _adminLog.Add(LogType.AdminMessage, LogImpact.Extreme, $"User {ToPrettyString(userEnt.Value)} try to request signature, but not an admin.");
            return;
        }

        var log = await _adminLog.GetJsonByLogId(args.LogId, args.Time);
        if (log == null)
        {
            _popup.PopupCursor(Loc.GetString("admin-logs-signature-popup-no-record-in-db"), userEnt.Value);
            return;
        }

        SignatureData? signature = null;

        var root = log.RootElement;
        foreach (var child in root.EnumerateObject())
        {
            if (child.Value.ValueKind != JsonValueKind.Object)
                continue;

            var obj = child.Value;

            if (!obj.TryGetProperty("serialized", out var serProp))
                continue;

            var serialized = serProp.GetString();
            if (string.IsNullOrEmpty(serialized))
                continue;

            var sig = SignatureData.Deserialize(serialized);
            if (sig == null)
                continue;

            signature = sig;
        }

        if (signature == null)
        {
            _popup.PopupCursor(Loc.GetString("admin-logs-signature-popup-cant-find-signature"), userEnt.Value);
            return;
        }

        var req = new SendSignatureToAdminEvent(signature);
        RaiseNetworkEvent(req, ev.SenderSession);
    }
}
