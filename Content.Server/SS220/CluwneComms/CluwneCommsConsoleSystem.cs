// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Communications;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.SS220.TTS;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Content.Shared.SS220.CluwneComms;
using Content.Server.Communications;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CluwneComms
{
    public sealed class CluwneCommsConsoleSystem : SharedCluwneCommsConsoleSystem
    {
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CluwneCommsConsoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<CluwneCommsConsoleComponent, CluwneCommsConsoleAnnounceMessage>(OnAnnounceMessage);
        }
        public void OnMapInit(Entity<CluwneCommsConsoleComponent> ent, ref MapInitEvent args)
        {
            ent.Comp.AnnouncementCooldownRemaining = _timing.CurTime + ent.Comp.Delay;
            ent.Comp.CanAnnounce = false;

            ent.Comp.AlertCooldownRemaining = _timing.CurTime + ent.Comp.Delay;
            ent.Comp.CanAlert = false;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<CluwneCommsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (!comp.CanAnnounce && _timing.CurTime >= comp.AnnouncementCooldownRemaining)
                {
                    comp.CanAnnounce = true;
                    UpdateUI(uid, comp);
                }

                if (!comp.CanAlert && _timing.CurTime >= comp.AlertCooldownRemaining)
                {
                    comp.CanAlert = true;
                    UpdateUI(uid, comp);
                }
            }
        }

        private void UpdateUI(EntityUid ent, CluwneCommsConsoleComponent comp)
        {
            List<string>? levels = null; //add here proto

            CluwneCommsConsoleInterfaceState newState = new CluwneCommsConsoleInterfaceState(comp.CanAnnounce, comp.CanAlert, levels);
            _uiSystem.SetUiState(ent, CluwneCommsConsoleUiKey.Key, newState);
        }

        private void OnAnnounceMessage(Entity<CluwneCommsConsoleComponent> ent, ref CluwneCommsConsoleAnnounceMessage message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message.Message, maxLength);
            var author = Loc.GetString("comms-console-announcement-unknown-sender");
            var voiceId = string.Empty;
            if (message.Actor is { Valid: true } mob)
            {
                if (!ent.Comp.CanAnnounce)
                    return;

                if (!CanUse(mob, ent))
                {
                    _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), ent, message.Actor);
                    return;
                }

                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, mob);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                author = tryGetIdentityShortInfoEvent.Title;

                if (TryComp<TTSComponent>(mob, out var tts))
                    voiceId = tts.VoicePrototypeId;
            }

            ent.Comp.AnnouncementCooldownRemaining = _timing.CurTime + ent.Comp.Delay;
            ent.Comp.CanAnnounce = false;
            Dirty(ent, ent.Comp);
            UpdateUI(ent, ent.Comp);


            // allow admemes with vv
            Loc.TryGetString(ent.Comp.Title, out var title);
            title ??= ent.Comp.Title;

            msg = _chatManager.DeleteProhibitedCharacters(msg, message.Actor);
            msg += "\n" + Loc.GetString("comms-console-announcement-sent-by") + " " + author;

            _chatSystem.DispatchStationAnnouncement(ent, msg, title, colorOverride: ent.Comp.Color, voiceId: voiceId);

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following station announcement: {msg}");
        }

        private bool CanUse(EntityUid user, EntityUid console)
        {
            if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent))
                return _accessReaderSystem.IsAllowed(user, console, accessReaderComponent);

            return true;
        }
    }
}
