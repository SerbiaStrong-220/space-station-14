using System.Globalization;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Communications;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Content.Shared.GameTicking;
using Content.Server.Communications;
using Content.Server.ConsoleNuke;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.PDA.Ringer;
using Content.Server.Traitor.Uplink;
using Content.Shared.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.NukeOperative;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using Content.Server.Stack;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Events;

namespace Content.Server.ConsoleNuke
{
    public sealed class ConsoleNukeSystem : EntitySystem
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        private bool _isWarStarted = false;
        public int WhenAbleToMove { get; private set; }

        public override void Initialize()
        {
            SubscribeLocalEvent<ConsoleNukeComponent, CommunicationConsoleUsed>(OnAnnounceMessage);
            SubscribeLocalEvent<ConsoleFTLAttemptEvent>(OnConsoleFTLAttempt);

            WhenAbleToMove = _cfg.GetCVar<int>("nuke.operative_defaulttime");
        }

        private void OnConsoleFTLAttempt(ref ConsoleFTLAttemptEvent ev)
        {
            if (_entityManager.TryGetComponent<ConsoleNukeComponent>(ev.Shuttle, out var comp))
            {
                var curTime = GetTime();
                if (WhenAbleToMove > curTime)
                {
                    ev.Reason = Loc.GetString("nuke-console-shuttle", ("time", WhenAbleToMove - curTime));
                    ev.Cancelled = true;
                }
            }
        }

        private int GetTime()
        {
            return (int) _gameTicker.RoundDuration().TotalMinutes;
        }

        private void OnAnnounceMessage(EntityUid uid, ConsoleNukeComponent comp,
            CommunicationConsoleUsed message)
        {
            if (_isWarStarted)
                return;

            if (GetTime() <= _cfg.GetCVar<int>("nuke.operative_defaulttime"))
            {
                if (message.Session.AttachedEntity is { Valid: true } player)
                {
                    var tc = _entityManager.CreateEntityUninitialized("Telecrystal");

                    if (_entityManager.TryGetComponent<StackComponent>(tc, out var component))
                    {
                        var stackSystem = _entitySystemManager.GetEntitySystem<StackSystem>();

                        int countTC = _entityManager.HasComponent<LoneNukeOperativeComponent>(player)
                            ? _cfg.GetCVar<int>("nuke.loneoperative_tc")
                            : _cfg.GetCVar<int>("nuke.operative_tc");

                        stackSystem.SetCount(component.Owner, countTC);

                        var handSystem = _entitySystemManager.GetEntitySystem<HandsSystem>();

                        _entityManager.InitializeAndStartEntity(tc);
                        handSystem.TryForcePickupAnyHand(player, tc);

                        _adminLogger.Add(LogType.WarReceiveTC, LogImpact.Medium, $"{ToPrettyString(player):player} has received {countTC} TC for declaration of war.");

                        goto end;
                    }
                    _entityManager.DeleteEntity(tc); // We should delete entity if something gone wrong
                }
            end:
                WhenAbleToMove += _cfg.GetCVar<int>("nuke.operative_additionaltime"); ;
                _isWarStarted = true;
            }
        }
    }
}
