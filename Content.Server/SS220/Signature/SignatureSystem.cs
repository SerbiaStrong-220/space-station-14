// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Shared.Database;
using Content.Shared.Paper;
using Content.Shared.Preferences;
using Content.Shared.SS220.Signature;
using Robust.Shared.Player;

namespace Content.Server.SS220.Signature;

public sealed class SignatureSystem : SharedSignatureSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestSignatureAdminMessage>(OnRequestSignatureAdmin);

        SubscribeLocalEvent<PaperComponent, ApplySavedSignatureMessage>(OnApplySavedSignature);
        SubscribeLocalEvent<SignatureComponent, SaveSignatureToProfileMessage>(OnSaveSignatureToProfile);
    }

    private async void OnRequestSignatureAdmin(RequestSignatureAdminMessage args, EntitySessionEventArgs ev)
    {
        var userEnt = ev.SenderSession.AttachedEntity;
        if (userEnt == null)
            return;

        if (!_adminManager.IsAdmin(ev.SenderSession, true))
            return;

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

            var sig = SignatureSerializer.Deserialize(serialized);
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

    // transfer this method from client, cause only the server must give a saved signature. But with this,
    // we create a little delay when the button clicks and appears signature in paper ui
    // TODO: remove this comment on merge
    private void OnApplySavedSignature(Entity<PaperComponent> ent, ref ApplySavedSignatureMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        var profile = _preferences.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter as HumanoidCharacterProfile;

        if (profile?.SignatureData == null)
            return;

        var state = new UpdateSignatureDataState(profile.SignatureData);
        _ui.SetUiState(ent.Owner, PaperComponent.PaperUiKey.Key, state);
    }

    // TODO: mb add a cooldown for saving the profile signature, cause right now a player can spam it and each click re-saves their character.
    // TODO: and each click run db command, really cant say nothing about optimization
    // p.s. cant find another pref method to save characters
    // TODO: remove this comment on merge
    private void OnSaveSignatureToProfile(Entity<SignatureComponent> ent, ref SaveSignatureToProfileMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        var userId = actor.PlayerSession.UserId;
        var session = actor.PlayerSession;
        var data = args.Data;

        var pref = _preferences.GetPreferences(userId);
        var character = pref.SelectedCharacter;
        var slot = pref.IndexOfCharacter(character);

        if (character is not HumanoidCharacterProfile humanoid)
            return;

        // if signatures are equals, then nothing to save
        if (humanoid.SignatureData != null && humanoid.SignatureData.Equals(data))
            return;

        _ = SaveAndResend();
        return;

        async Task SaveAndResend()
        {
            var updated = humanoid.WithSignatureData(data);
            await _preferences.SetProfile(userId, slot, updated);

            // we need to sync server pref with a client pref
            _preferences.FinishLoad(session);
        }
    }

    protected override void AfterSubmitSignature(Entity<PaperComponent, SignatureComponent> ent, ref SignatureSubmitMessage args, bool changedSignature)
    {
        base.AfterSubmitSignature(ent, ref args, changedSignature);

        var verboseChangedSignature = changedSignature ? "changed signature" : "written without changing signature";

        if (ent.Comp2.Data is not null)
            _adminLog.Add(LogType.Chat, LogImpact.Medium, $"{ToPrettyString(args.Actor):user} has {verboseChangedSignature} {new SignatureLogData(ent.Comp2.Data)} on {ToPrettyString(ent):target}");
    }
}
