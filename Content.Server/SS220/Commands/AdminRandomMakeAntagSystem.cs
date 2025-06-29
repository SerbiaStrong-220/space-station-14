// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Server.Administration;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Random;
using Content.Shared.Ghost;
using Content.Server.Administration.Managers;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Preferences;
using Content.Server.Preferences.Managers;

namespace Content.Server.SS220.Commands
{
    [AdminCommand(AdminFlags.VarEdit)] // Only for admins
    public sealed class MakeAntagCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IServerPreferencesManager _pref = default!;

        public string Command => "makerandomantag";
        public string Description => Loc.GetString("command-makerandomantag-description");
        public string Help => $"Usage: {Command}";

        private readonly List<string> _antagTypes = new() // TODO: When will add a cult add a cultist there
        {
            "Traitor",
            "Thief",
            "InitialInfected",
        };

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0 || !_antagTypes.Contains(args[0]))
            {
                shell.WriteLine(Loc.GetString("command-makerandomantag-objective"));
                return;
            }

            var successEntityUid = AdminMakeRandomAntagCommand(args[0]);

            if (successEntityUid != null)
            {
                shell.WriteLine(Loc.GetString("command-makerandomantag-sucess",
                    ("Entityname", Identity.Name(successEntityUid.Value, _entityManager)), ("antag", args[0])));
            }
            else
                shell.WriteLine(Loc.GetString("command-makerandomantag-negative"));
        }

        private EntityUid? AdminMakeRandomAntagCommand(string defaultRule)
        {
            var antag = _entityManager.System<AntagSelectionSystem>();
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var gameTicker = _entityManager.System<GameTicker>();
            var random = IoCManager.Resolve<IRobustRandom>();
            var mindSystem = _entityManager.System<SharedMindSystem>();
            var roleSystem = _entityManager.System<RoleSystem>();
            var banSystem = IoCManager.Resolve<IBanManager>();

            var players = playerManager.Sessions
                .Where(x => gameTicker.PlayerGameStatuses[x.UserId] == PlayerGameStatus.JoinedGame)
                .ToList();

            random.Shuffle(players); // Shuffle player list to be more randomly

            foreach (var player in players)
            {

                var pref = (HumanoidCharacterProfile)_pref.GetPreferences(player.UserId).SelectedCharacter;

                if (!mindSystem.TryGetMind(player.UserId, out var mindId)) // Is it player or a cow?
                    continue;

                if (banSystem.GetRoleBans(player.UserId) is { } roleBans &&
                    roleBans.Contains("Job:" + defaultRule)) // Do he have a roleban on THIS antag?
                    continue;

                if (roleSystem.MindIsAntagonist(mindId) ||
                _entityManager.HasComponent<GhostComponent>(player.AttachedEntity) ||
                _entityManager.HasComponent<MindShieldComponent>(player.AttachedEntity)) // Is he already lucky boy or he is ghost or he is CAPTIAN?
                    continue;

                if (!pref.AntagPreferences.Contains(defaultRule)) // Do he want to be a chosen antag or no?
                    continue;

                switch (defaultRule) // TODO: When will add a cult add a cultist there too. U can add more for fun if u want.
                {
                    case "Traitor":
                        antag.ForceMakeAntag<TraitorRuleComponent>(player, defaultRule);
                        break;
                    case "Thief":
                        antag.ForceMakeAntag<ThiefRuleComponent>(player, defaultRule);
                        break;
                    case "InitialInfected":
                        antag.ForceMakeAntag<ZombieRuleComponent>(player, defaultRule);
                        break;
                }
                if (roleSystem.MindIsAntagonist(mindId)) // If he sucessfuly passed all checks and get his antag?
                    return player.AttachedEntity;
            }
            return null;
        }


        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromOptions(_antagTypes);
            }
            return CompletionResult.Empty;
        }
    }
}
