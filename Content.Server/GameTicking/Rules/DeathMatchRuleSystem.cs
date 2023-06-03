using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Content.Server.PDA.Ringer;
using Content.Server.Players;
using Content.Server.Traitor;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Manages <see cref="DeathMatchRuleComponent"/>
/// </summary>
public sealed class DeathMatchRuleSystem : GameRuleSystem<DeathMatchRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageChangedEvent>(OnHealthChanged);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    protected override void Started(EntityUid uid, DeathMatchRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-death-match-added-announcement"));

    }

    protected override void Ended(EntityUid uid, DeathMatchRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        component.DeadCheckTimer = null;
        component.RestartTimer = null;

    }

    private void OnHealthChanged(DamageChangedEvent _)
    {
        RunDelayedCheck();
    }

    private void OnPlayerStatusChanged(object? ojb, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            RunDelayedCheck();
        }
    }

    private void RunDelayedCheck()
    {
        var query = EntityQueryEnumerator<DeathMatchRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var deathMatch, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule) || deathMatch.DeadCheckTimer != null)
                continue;

            deathMatch.DeadCheckTimer = deathMatch.DeadCheckDelay;
        }
    }

    protected override void ActiveTick(EntityUid uid, DeathMatchRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        foreach (var traitor in _playerManager.ServerSessions)
        {
            MakeTraitor(traitor);
        }

        // If the restart timer is active, that means the round is ending soon, no need to check for winners.
        // TODO: We probably want a sane, centralized round end thingie in GameTicker, RoundEndSystem is no good...
        if (component.RestartTimer != null)
        {
            component.RestartTimer -= frameTime;

            if (component.RestartTimer > 0f)
                return;

            GameTicker.EndRound();
            GameTicker.RestartRound();
            return;
        }

        if (!_cfg.GetCVar(CCVars.GameLobbyEnableWin) || component.DeadCheckTimer == null)
            return;

        component.DeadCheckTimer -= frameTime;

        if (component.DeadCheckTimer > 0)
            return;

        component.DeadCheckTimer = null;

        IPlayerSession? winner = null;
        foreach (var playerSession in _playerManager.ServerSessions)
        {
            if (playerSession.AttachedEntity is not { Valid: true } playerEntity
                || !TryComp(playerEntity, out MobStateComponent? state))
                continue;

            if (!_mobStateSystem.IsAlive(playerEntity, state))
                continue;

            // Found a second person alive, nothing decided yet!
            if (winner != null)
                return;

            winner = playerSession;
        }

        _chatManager.DispatchServerAnnouncement(winner == null
            ? Loc.GetString("rule-death-match-check-winner-stalemate")
            : Loc.GetString("rule-death-match-check-winner", ("winner", winner)));

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds",
            ("seconds", component.RestartDelay)));
        component.RestartTimer = component.RestartDelay;
    }

    public bool MakeTraitor(IPlayerSession traitor)
    {
        var traitorRule = EntityQuery<TraitorRuleComponent>().FirstOrDefault();
        if (traitorRule == null)
        {
            //todo fuck me this shit is awful
            //no i wont fuck you, erp is against rules
            GameTicker.StartGameRule("Traitor", out var ruleEntity);
            traitorRule = Comp<TraitorRuleComponent>(ruleEntity);
        }

        var mind = traitor.Data.ContentData()?.Mind;
        if (mind == null)
        {
            _sawmill.Info("Failed getting mind for picked traitor.");
            return false;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            Logger.ErrorS("preset", "Mind picked for traitor did not have an attached entity.");
            return false;
        }

        // creadth: we need to create uplink for the antag.
        // PDA should be in place already
        DebugTools.AssertNotNull(mind.OwnedEntity);

        var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);

        var pda = _uplink.FindUplinkTarget(mind.OwnedEntity!.Value);
        if (pda == null || !_uplink.AddUplink(mind.OwnedEntity.Value, startingBalance))
            return false;

        // add the ringtone uplink and get its code for greeting
        var code = AddComp<RingerUplinkComponent>(pda.Value).Code;

        var antagPrototype = _prototypeManager.Index<AntagPrototype>(traitorRule.TraitorPrototypeId);
        var traitorRole = new TraitorRole(mind, antagPrototype);
        mind.AddRole(traitorRole);
        traitorRole.GreetTraitor(traitorRule.Codewords, code);

        var maxDifficulty = _cfg.GetCVar(CCVars.TraitorMaxDifficulty);
        var maxPicks = _cfg.GetCVar(CCVars.TraitorMaxPicks);

        //give traitors their codewords and uplink code to keep in their character info menu
        traitorRole.Mind.Briefing = Loc.GetString("traitor-role-codewords-short", ("codewords", string.Join(", ", traitorRule.Codewords)))
            + "\n" + Loc.GetString("traitor-role-uplink-code-short", ("code", string.Join("", code)));

        return true;
    }
}
