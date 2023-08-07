using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.NPC.Systems;
using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.PDA.Ringer;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Server.Traitor.Uplink;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Preferences;
using Content.Shared.Mobs.Systems;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class KontrRazvedchikRuleSystem : GameRuleSystem<KontrRazvedchikRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    private ISawmill _sawmill = default!;

    private int PlayersPerKontra => _cfg.GetCVar(CCVars.KontraPlayersPerKontra);
    private int MaxKontras => _cfg.GetCVar(CCVars.KontraMaxKontras);

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("preset");

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    protected override void ActiveTick(EntityUid uid, KontrRazvedchikRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus == KontrRazvedchikRuleComponent.SelectionState.ReadyToSelect && _gameTiming.CurTime > component.AnnounceAt)
            DoKontraStart(component);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<KontrRazvedchikRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var kontra, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            MakeCodewords(kontra);

            var minPlayers = _cfg.GetCVar(CCVars.KontraMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("kontra-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
                ev.Cancel();
                continue;
            }

            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("kontra-no-one-ready"));
                ev.Cancel();
            }
        }
    }

    private void MakeCodewords(KontrRazvedchikRuleComponent component)
    {
        var codewordCount = _cfg.GetCVar(CCVars.TraitorCodewordCount);
        var adjectives = _prototypeManager.Index<DatasetPrototype>("adjectives").Values;
        var verbs = _prototypeManager.Index<DatasetPrototype>("verbs").Values;
        var codewordPool = adjectives.Concat(verbs).ToList();
        var finalCodewordCount = Math.Min(codewordCount, codewordPool.Count);
        component.Codewords = new string[finalCodewordCount];
        for (var i = 0; i < finalCodewordCount; i++)
        {
            component.Codewords[i] = _random.PickAndTake(codewordPool);
        }
    }

    private void DoKontraStart(KontrRazvedchikRuleComponent component)
    {
        if (!component.StartCandidates.Any())
        {
            _sawmill.Error("Tried to start Kontra mode without any candidates.");
            return;
        }

        var numKontras = MathHelper.Clamp(component.StartCandidates.Count / PlayersPerKontra, 1, MaxKontras);
        var kontraPool = FindPotentialKontras(component.StartCandidates, component);
        var selectedKontras = PickKontras(numKontras, kontraPool);

        foreach (var kontra in selectedKontras)
        {
            MakeKontrrazvedchik(kontra);
        }

        component.SelectionStatus = KontrRazvedchikRuleComponent.SelectionState.SelectionMade;
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<KontrRazvedchikRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;

                traitor.StartCandidates[player] = ev.Profiles[player.UserId];
            }

            var delay = TimeSpan.FromSeconds(
                _cfg.GetCVar(CCVars.TraitorStartDelay) +
                _random.NextFloat(0f, _cfg.GetCVar(CCVars.TraitorStartDelayVariance)));

            traitor.AnnounceAt = _gameTiming.CurTime + delay;

            traitor.SelectionStatus = KontrRazvedchikRuleComponent.SelectionState.ReadyToSelect;
        }
    }

    private List<IPlayerSession> FindPotentialKontras(in Dictionary<IPlayerSession, HumanoidCharacterProfile> candidates, KontrRazvedchikRuleComponent component)
    {
        var list = new List<IPlayerSession>();
        var pendingQuery = GetEntityQuery<PendingClockInComponent>();

        foreach (var player in candidates.Keys)
        {
            // Role prevents antag.
            if (!(player.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false))
            {
                continue;
            }

            // Latejoin
            if (player.AttachedEntity != null && pendingQuery.HasComponent(player.AttachedEntity.Value))
                continue;

            list.Add(player);
        }

        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            var profile = candidates[player];
            if (profile.AntagPreferences.Contains(component.KontraPrototypeId))
            {
                prefList.Add(player);
            }
        }
        if (prefList.Count == 0)
        {
            _sawmill.Info("Insufficient preferred kontras, picking at random.");
            prefList = list;
        }
        return prefList;
    }

    private List<IPlayerSession> PickKontras(int kontraCount, List<IPlayerSession> prefList)
    {
        var results = new List<IPlayerSession>(kontraCount);
        if (prefList.Count == 0)
        {
            _sawmill.Info("Insufficient ready players to fill up with kontras, stopping the selection.");
            return results;
        }

        for (var i = 0; i < kontraCount; i++)
        {
            results.Add(_random.PickAndTake(prefList));
            _sawmill.Info("Selected a preferred kontra.");
        }
        return results;
    }

    public bool MakeKontrrazvedchik(IPlayerSession kontra)
    {
        var KontraRule = EntityQuery<KontrRazvedchikRuleComponent>().FirstOrDefault();
        if (KontraRule == null)
        {
            //todo fuck me this shit is awful
            //no i wont fuck you, erp is against rules
            GameTicker.StartGameRule("Kontrrazvedka", out var ruleEntity);
            KontraRule = Comp<KontrRazvedchikRuleComponent>(ruleEntity);
            MakeCodewords(KontraRule);
        }

        var mind = kontra.Data.ContentData()?.Mind;
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

        // Calculate the amount of currency on the uplink.
        var startingBalance = _cfg.GetCVar(CCVars.TraitorStartingBalance);
        if (mind.CurrentJob != null)
            startingBalance = Math.Max(startingBalance - mind.CurrentJob.Prototype.AntagAdvantage, 0);

        // creadth: we need to create uplink for the antag.
        // PDA should be in place already
        var pda = _uplink.FindUplinkTarget(mind.OwnedEntity!.Value);
        if (pda == null || !_uplink.AddUplink(mind.OwnedEntity.Value, startingBalance))
            return false;

        // Add the ringtone uplink and get its code for greeting
        var code = EnsureComp<RingerUplinkComponent>(pda.Value).Code;

        // Prepare antagonist role
        var protoPrototype = _prototypeManager.Index<ProtogonistPrototype>(KontraRule.KontraPrototypeId);
        var kontrRole = new KontrrazvedchikRole(mind, protoPrototype);

        // Give traitors their codewords and uplink code to keep in their character info menu
        kontrRole.Mind.Briefing = string.Format(
            "{0}\n{1}",
            Loc.GetString("kontra-role-codewords-short", ("codewords", string.Join(", ", KontraRule.Codewords))),
            Loc.GetString("kontra-role-uplink-code-short", ("code", string.Join("-", code).Replace("sharp", "#"))));

        // Assign traitor roles
        _mindSystem.AddRole(mind, kontrRole);
        SendKontrasBriefing(mind, KontraRule.Codewords, code);
        KontraRule.Kontrs.Add(kontrRole);

        if (_mindSystem.TryGetSession(mind, out var session))
        {
            // Notificate player about new role assignment
            _audioSystem.PlayGlobal(KontraRule.GreetSoundNotification, session);
        }

        // Give traitors their objectives
        var maxDifficulty = _cfg.GetCVar(CCVars.TraitorMaxDifficulty);
        var maxPicks = _cfg.GetCVar(CCVars.TraitorMaxPicks);
        var difficulty = 0f;
        for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
        {
            var objective = _objectivesManager.GetRandomObjective(kontrRole.Mind, "KontraObjectiveGroups");

            if (objective == null)
                continue;
            if (_mindSystem.TryAddObjective(kontrRole.Mind, objective))
                difficulty += objective.Difficulty;
        }

        return true;
    }

    /// <summary>
    ///     Send a codewords and uplink codes to traitor chat.
    /// </summary>
    /// <param name="mind">A mind (player)</param>
    /// <param name="codewords">Codewords</param>
    /// <param name="code">Uplink codes</param>
    private void SendKontrasBriefing(Mind.Mind mind, string[] codewords, Note[] code)
    {
        if (_mindSystem.TryGetSession(mind, out var session))
        {
            _chatManager.DispatchServerMessage(session, Loc.GetString("kontra-role-greeting"));
            _chatManager.DispatchServerMessage(session, Loc.GetString("kontra-role-codewords", ("codewords", string.Join(", ", codewords))));
            _chatManager.DispatchServerMessage(session, Loc.GetString("kontra-role-uplink-code", ("code", string.Join("-", code).Replace("sharp", "#"))));
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<KontrRazvedchikRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var kontra, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (kontra.TotalKontras >= MaxKontras)
                continue;
            if (!ev.LateJoin)
                continue;
            if (!ev.Profile.AntagPreferences.Contains(kontra.KontraPrototypeId))
                continue;

            if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
                continue;

            if (!job.CanBeAntag)
                continue;

            // Before the announcement is made, late-joiners are considered the same as players who readied.
            if (kontra.SelectionStatus < KontrRazvedchikRuleComponent.SelectionState.SelectionMade)
            {
                kontra.StartCandidates[ev.Player] = ev.Profile;
                continue;
            }

            // the nth player we adjust our probabilities around
            var target = PlayersPerKontra * kontra.TotalKontras + 1;

            var chance = 1f / PlayersPerKontra;

            // If we have too many traitors, divide by how many players below target for next traitor we are.
            if (ev.JoinOrder < target)
            {
                chance /= (target - ev.JoinOrder);
            }
            else // Tick up towards 100% chance.
            {
                chance *= ((ev.JoinOrder + 1) - target);
            }

            if (chance > 1)
                chance = 1;

            // Now that we've calculated our chance, roll and make them a traitor if we roll under.
            // You get one shot.
            if (_random.Prob(chance))
            {
                MakeKontrrazvedchik(ev.Player);
            }
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<KontrRazvedchikRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var kontra, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var result = Loc.GetString("kontra-round-end-result", ("kontraCount", kontra.Kontrs.Count));

            result += "\n" + Loc.GetString("kontra-round-end-codewords", ("codewords", string.Join(", ", kontra.Codewords))) +
                      "\n";

            foreach (var t in kontra.Kontrs)
            {
                var name = t.Mind.CharacterName;
                _mindSystem.TryGetSession(t.Mind, out var session);
                var username = session?.Name;

                var objectives = t.Mind.AllObjectives.ToArray();
                if (objectives.Length == 0)
                {
                    if (username != null)
                    {
                        if (name == null)
                            result += "\n" + Loc.GetString("kontra-user-was-a-kontra", ("user", username));
                        else
                            result += "\n" + Loc.GetString("kontra-user-was-a-kontra-named", ("user", username),
                                ("name", name));
                    }
                    else if (name != null)
                        result += "\n" + Loc.GetString("kontra-was-a-kontra-named", ("name", name));

                    continue;
                }

                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("kontra-user-was-a-kontra-with-objectives",
                            ("user", username));
                    else
                        result += "\n" + Loc.GetString("kontra-user-was-a-kontra-with-objectives-named",
                            ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("kontra-was-a-kontra-with-objectives-named", ("name", name));

                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    result += "\n" + Loc.GetString($"preset-kontra-objective-issuer-{objectiveGroup.Key}");

                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            if (progress > 0.99f)
                            {
                                result += "\n- " + Loc.GetString(
                                    "kontra-objective-condition-success",
                                    ("condition", condition.Title),
                                    ("markupColor", "green")
                                );
                            }
                            else
                            {
                                result += "\n- " + Loc.GetString(
                                    "kontra-objective-condition-fail",
                                    ("condition", condition.Title),
                                    ("progress", (int) (progress * 100)),
                                    ("markupColor", "red")
                                );
                            }
                        }
                    }
                }
            }

            ev.AddLine(result);
        }
    }

    public List<KontrrazvedchikRole> GetOtherKontrasAliveAndConnected(Mind.Mind ourMind)
    {
        List<KontrrazvedchikRole> allKontras = new();
        foreach (var kontra in EntityQuery<KontrRazvedchikRuleComponent>())
        {
            foreach (var role in GetOtherKontrasAliveAndConnected(ourMind, kontra))
            {
                if (!allKontras.Contains(role))
                    allKontras.Add(role);
            }
        }

        return allKontras;
    }

    private List<KontrrazvedchikRole> GetOtherKontrasAliveAndConnected(Mind.Mind ourMind, KontrRazvedchikRuleComponent component)
    {
        return component.Kontrs // don't want
            .Where(t => t.Mind.OwnedEntity is not null) // no entity
            .Where(t => t.Mind.Session is not null) // player disconnected
            .Where(t => t.Mind != ourMind) // ourselves
            .Where(t => _mobStateSystem.IsAlive((EntityUid) t.Mind.OwnedEntity!)) // dead
            .Where(t => t.Mind.CurrentEntity == t.Mind.OwnedEntity).ToList(); // not in original body
    }
}
