// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.SS220.Commands;

[AdminCommand(AdminFlags.VarEdit)] // Only for admins
public sealed class MakeAntagCommand : IConsoleCommand
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IBanManager _banManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IServerPreferencesManager _pref = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public string Command => "makerandomantag";
    public string Description => Loc.GetString("command-makerandomantag-description");
    public string Help => $"Usage: {Command}";

    private readonly List<string> _antagTypes =
    // TODO: When will add a cult add a cultist there
    [
        "Traitor",
        "Thief",
        "InitialInfected",
        "CultistOfYoggSothoth"
    ];

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
        var players = _playerManager.Sessions
            .Where(x => _gameTicker.PlayerGameStatuses[x.UserId] == PlayerGameStatus.JoinedGame)
            .ToList();

        _random.Shuffle(players); // Shuffle player list to be more randomly

        foreach (var player in players)
        {
            var pref = (HumanoidCharacterProfile)_pref.GetPreferences(player.UserId).SelectedCharacter;

            if (!_mindSystem.TryGetMind(player.UserId, out var mindId)) // Is it player or a cow?
                continue;

            if (_banManager.GetRoleBans(player.UserId) is { } roleBans &&
                roleBans.Contains("Job:" + defaultRule)) // Do he have a roleban on THIS antag?
                continue;

            if (_role.MindIsAntagonist(mindId))//no double antaging
                continue;

            if (_entityManager.HasComponent<GhostComponent>(player.AttachedEntity))//ghost cant be antag
                continue;

            if (_entityManager.HasComponent<MindShieldComponent>(player.AttachedEntity))//no no for antag roles
                continue;

            if (_entityManager.HasComponent<AntagImmuneComponent>(player.AttachedEntity))//idk what is this, obr?
                continue;

            if (!pref.AntagPreferences.Contains(defaultRule)) // Do he want to be a chosen antag or no?
                continue;

            switch (defaultRule) // TODO: When will add a cult add a cultist there too. U can add more for fun if u want.
            {
                case "Traitor":
                    _antag.ForceMakeAntag<TraitorRuleComponent>(player, defaultRule);
                    break;
                case "Thief":
                    _antag.ForceMakeAntag<ThiefRuleComponent>(player, defaultRule);
                    break;
                case "InitialInfected":
                    _antag.ForceMakeAntag<ZombieRuleComponent>(player, defaultRule);
                    break;
                case "CultistOfYoggSothoth":
                    _antag.ForceMakeAntag<CultYoggRuleComponent>(player, defaultRule);
                    break;
            }

            if (_role.MindIsAntagonist(mindId)) // If he sucessfuly passed all checks and get his antag?
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
