// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.SS220.TTS;
using Robust.Shared.Configuration;
using Content.Shared.SS220.CluwneComms;
using Robust.Shared.Timing;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Shared.MassMedia.Systems;
using Content.Server.GameTicking;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.Explosion.EntitySystems;
using Content.Server.MassMedia.Components;
using Content.Shared.MassMedia.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Station.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.SS220.CluwneComms
{
    public sealed class CluwneCommsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CluwneCommsConsoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<CluwneCommsConsoleComponent, CluwneCommsConsoleAnnounceMessage>(OnAnnounceMessage);
            SubscribeLocalEvent<CluwneCommsConsoleComponent, CluwneCommsConsoleAlertMessage>(OnAlertMessage);
            SubscribeLocalEvent<CluwneCommsConsoleComponent, CluwneCommsConsoleBoomMessage>(OnBoomMessage);
        }
        public void OnMapInit(Entity<CluwneCommsConsoleComponent> ent, ref MapInitEvent args)
        {
            //we set timers so that it is impossible to abuse the console rebuild
            ent.Comp.AnnouncementCooldownRemaining = _timing.CurTime + ent.Comp.InitialAnnounceDelay;
            ent.Comp.CanAnnounce = false;

            ent.Comp.AlertCooldownRemaining = _timing.CurTime + ent.Comp.InitialAlertDelay;
            ent.Comp.CanAlert = false;

            //set memelert from proto
            foreach (var memelert in _prototypeManager.EnumeratePrototypes<MemelertLevelPrototype>())
            {
                ent.Comp.LevelsDict.Add(memelert.ID, memelert);
            }
            UpdateUI(ent, ent.Comp);

            //create a station component in case it is not present at the station
            var station = _station.GetOwningStation(ent);
            if (!station.HasValue)
                return;

            EnsureComp<StationNewsComponent>(station.Value);
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
                    comp.AnnouncementCooldownRemaining = null;
                    UpdateUI(uid, comp);
                }

                if (!comp.CanAlert && _timing.CurTime >= comp.AlertCooldownRemaining)
                {
                    comp.CanAlert = true;
                    comp.AlertCooldownRemaining = null;
                    UpdateUI(uid, comp);
                }
            }
        }

        private void UpdateUI(EntityUid ent, CluwneCommsConsoleComponent comp)
        {
            List<string>? levels = new();

            foreach (var item in comp.LevelsDict)
            {
                levels.Add(item.Key);
            }

            CluwneCommsConsoleInterfaceState newState = new CluwneCommsConsoleInterfaceState(comp.CanAnnounce, comp.CanAlert, levels, comp.AnnouncementCooldownRemaining, comp.AlertCooldownRemaining);
            _uiSystem.SetUiState(ent, CluwneCommsConsoleUiKey.Key, newState);
        }

        private void OnAnnounceMessage(Entity<CluwneCommsConsoleComponent> ent, ref CluwneCommsConsoleAnnounceMessage args)
        {
            if (args.Message == "")
            {
                _audio.PlayEntity(ent.Comp.DenySound, args.Actor, args.Actor);
                return;
            }

            if (!ent.Comp.CanAnnounce)
                return;

            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(args.Message, maxLength);
            var author = Loc.GetString("cluwne-comms-console-announcement-unknown-sender");
            var voiceId = string.Empty;

            if (args.Actor is { Valid: true } mob)
            {
                if (!CanUse(mob, ent))
                {
                    _popupSystem.PopupEntity(Loc.GetString("cluwne-comms-console-permission-denied"), ent, args.Actor);
                    return;
                }

                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, mob);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                author = tryGetIdentityShortInfoEvent.Title;

                if (TryComp<TTSComponent>(mob, out var tts))
                    voiceId = tts.VoicePrototypeId;
            }

            // allow admemes with vv
            Loc.TryGetString(ent.Comp.Title, out var title);
            title ??= ent.Comp.Title;

            msg = _chatManager.DeleteProhibitedCharacters(msg, args.Actor);
            msg += "\n" + Loc.GetString("cluwne-comms-console-announcement-sent-by") + author;

            _chatSystem.DispatchStationAnnouncement(ent, msg, title, true, ent.Comp.Sound, colorOverride: ent.Comp.Color, voiceId);

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(args.Actor):player} has sent the following station announcement: {msg}");

            ent.Comp.AnnouncementCooldownRemaining = _timing.CurTime + ent.Comp.AnnounceDelay;
            ent.Comp.CanAnnounce = false;
            UpdateUI(ent, ent.Comp);
        }

        private void OnAlertMessage(Entity<CluwneCommsConsoleComponent> ent, ref CluwneCommsConsoleAlertMessage args)
        {
            if (args.Message == "" || args.Instruntions == "" || args.Alert == "")
            {
                _audio.PlayEntity(ent.Comp.DenySound, args.Actor, args.Actor);
                return;
            }

            if (!ent.Comp.LevelsDict.TryGetValue(args.Alert, out var alertInfo))
                return;

            //alert announce from AlertLevelSystem
            _audio.PlayGlobal(alertInfo.LevelDetails.Sound, Filter.Broadcast(), true);

            _chatSystem.DispatchStationAnnouncement(ent, args.Message, colorOverride: alertInfo.LevelDetails.Color);

            //Intructions from console
            //partly copied from NewsSystem
            if (!TryGetArticles(ent, out var articles))
                return;

            var author = Loc.GetString("cluwne-comms-console-announcement-unknown-sender");
            if (args.Actor is { Valid: true } mob)
            {
                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, mob);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                author = tryGetIdentityShortInfoEvent.Title;
            }

            var article = new NewsArticle
            {
                Title = Loc.GetString($"joke-alert-level-{args.Alert}-news-title"),
                Content = args.Instruntions.Trim(),
                Author = author,
                ShareTime = _ticker.RoundDuration()
            };

            _adminLogger.Add(LogType.Chat, LogImpact.Medium, $"{ToPrettyString(args.Actor):actor} created news article {article.Title} by {article.Author}: {article.Content}");

            _chatManager.SendAdminAnnouncement(Loc.GetString("news-publish-admin-announcement",
                ("actor", args.Actor),
                ("title", article.Title),
                ("author", article.Author ?? Loc.GetString("news-read-ui-no-author"))
                ));

            articles.Add(article);

            var ev = new NewsArticlePublishedEvent(article);
            var query = EntityQueryEnumerator<NewsReaderCartridgeComponent>();
            while (query.MoveNext(out var readerUid, out _))
            {
                RaiseLocalEvent(readerUid, ref ev);
            }

            ent.Comp.AlertCooldownRemaining = _timing.CurTime + ent.Comp.AlertDelay;
            ent.Comp.CanAlert = false;

            UpdateUI(ent, ent.Comp);
            UpdateWriterDevices();
        }

        private bool CanUse(EntityUid user, EntityUid console)
        {
            if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent))
                return _accessReaderSystem.IsAllowed(user, console, accessReaderComponent);

            return true;
        }

        private void OnBoomMessage(Entity<CluwneCommsConsoleComponent> ent, ref CluwneCommsConsoleBoomMessage args)
        {
            if (_random.Prob(ent.Comp.ExplosionProbability))//in case somebody abuse it just as bomb
            {                                                            //={}
                _uiSystem.CloseUi(ent.Owner, CluwneCommsConsoleUiKey.Key, args.Actor);
                _explosion.QueueExplosion(ent, "Default", ent.Comp.ExplosionTotalIntensity, ent.Comp.ExplosionSlope, ent.Comp.ExplosionMaxTileIntensity, canCreateVacuum: false);
            }
            else
                _audio.PlayPvs(ent.Comp.BoomFailSound, ent);
        }

        #region News copypaste
        //Copypaste from NewsSystem because original methods are private

        private void UpdateWriterUi(Entity<NewsWriterComponent> ent)
        {
            if (!_ui.HasUi(ent, NewsWriterUiKey.Key))
                return;

            if (!TryGetArticles(ent, out var articles))
                return;

            var state = new NewsWriterBoundUserInterfaceState(articles.ToArray(), ent.Comp.PublishEnabled, ent.Comp.NextPublish, ent.Comp.DraftTitle, ent.Comp.DraftContent);
            _ui.SetUiState(ent.Owner, NewsWriterUiKey.Key, state);
        }

        private bool TryGetArticles(EntityUid uid, [NotNullWhen(true)] out List<NewsArticle>? articles)
        {
            if (_station.GetOwningStation(uid) is not { } station ||
                !TryComp<StationNewsComponent>(station, out var stationNews))
            {
                articles = null;
                return false;
            }

            articles = stationNews.Articles;
            return true;
        }

        private void UpdateWriterDevices()
        {
            var query = EntityQueryEnumerator<NewsWriterComponent>();
            while (query.MoveNext(out var owner, out var comp))
            {
                UpdateWriterUi((owner, comp));
            }
        }
        #endregion
    }
}
