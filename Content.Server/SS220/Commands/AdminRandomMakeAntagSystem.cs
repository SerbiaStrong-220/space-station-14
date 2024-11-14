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
using Content.Server.Roles.Jobs;
using Content.Server.Preferences.Managers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.Antag.Components;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Content.Shared.Mind;

namespace Content.Server.SS220.Commands
{
    [AdminCommand(AdminFlags.Ban)] // Only for admins
    public sealed class MakeAntagCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "makerandomantag";
        public string Description => "Делает случайного игрока антагонистом из предложенного списка с учетом джобок.";
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
                shell.WriteLine("Необходимо указать тип антагониста: Traitor, Thief, или InitialInfected.");
                return;
            }

            var isSuccessEntityUid = AdminMakeRandomAntagCommand(args[0]);

            if (isSuccessEntityUid != null)
            {
                shell.WriteLine($"{Identity.Name(isSuccessEntityUid.Value, _entityManager)} успешно стал {args[0]}");
            }
            else
                shell.WriteLine($"Никто не стал антагонистом, потому что все итак антагонисты! Ну или в джоббане..");
        }

        private EntityUid? AdminMakeRandomAntagCommand(string defaultRule)
        {
            var antag = _entityManager.System<AntagSelectionSystem>();
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var gameTicker = _entityManager.System<GameTicker>();
            var robust = IoCManager.Resolve<IRobustRandom>();
            var mindSystem = _entityManager.System<SharedMindSystem>();
            var roleSystem = _entityManager.System<RoleSystem>();
            var banSystem = IoCManager.Resolve<IBanManager>();

            var players = playerManager.Sessions
                .Where(x => gameTicker.PlayerGameStatuses[x.UserId] == PlayerGameStatus.JoinedGame)
                .ToList();

            var playersPool = new AntagSelectionPlayerPool([players]);

            playersPool.TryPickAndTake(robust, out var session);

            if (session == null)
                return null;

            mindSystem.TryGetMind(session.UserId, out var mindId);

            if (mindId == null)
                return null;

            var banRole = banSystem.GetRoleBans(session.UserId);
            if (banSystem.GetRoleBans(session.UserId) is { } roleBans)
            {
                if (roleBans.Contains("Job:" + defaultRule))
                    return null;
            }

            if (roleSystem.MindHasRole<TraitorRoleComponent>(mindId.Value) ||
           roleSystem.MindHasRole<ThiefRoleComponent>(mindId.Value) ||
            roleSystem.MindHasRole<ZombieRoleComponent>(mindId.Value) ||
            _entityManager.TryGetComponent<GhostComponent>(session.AttachedEntity, out var Ghostcomp))
                return null;

            switch (defaultRule) // TODO: When will add a cult add a cultist there too. U can add more for fun if u want.
            {
                case "Traitor":
                    antag.ForceMakeAntag<TraitorRuleComponent>(session, defaultRule);
                    break;
                case "Thief":
                    antag.ForceMakeAntag<ThiefRuleComponent>(session, defaultRule);
                    break;
                case "InitialInfected":
                    antag.ForceMakeAntag<ZombieRuleComponent>(session, defaultRule);
                    break;
            }

            return session.AttachedEntity;
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
