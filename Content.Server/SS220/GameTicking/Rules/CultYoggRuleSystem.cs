// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Rules;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Chat.Managers;
using Content.Server.Zombies;
using Content.Server.Mind;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Content.Server.Antag;
using Content.Shared.SS220.CultYogg;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.GameTicking.Components;
using Content.Server.GameTicking;

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class CultYoggRuleSystem : GameRuleSystem<CultYoggRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<MiGoComponent, MiGoEnslavetDoAfterEvent>(MiGoEnslave);
    }

    private void AfterEntitySelected(Entity<CultYoggRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, ent);
    }
    private void MiGoEnslave(Entity<MiGoComponent> uid, ref MiGoEnslavetDoAfterEvent args)// Make somebody a cultist
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        GetCultGamerule(out var gameRuleEntity, out var gameRule);

        if (gameRule == null)
            return;

        MakeCultist((EntityUid) args.Target, gameRule);

        args.Handled = true;
    }

    private void GetCultGamerule(out EntityUid? ruleEntity, out CultYoggRuleComponent? component)
    {
        var gameRules = _gameTicker.GetActiveGameRules().GetEnumerator();
        ruleEntity = null;
        while (gameRules.MoveNext())
        {
            if (!HasComp<CultYoggRuleComponent>(gameRules.Current))
                continue;

            ruleEntity = gameRules.Current;
            break;
        }

        TryComp(ruleEntity, out component);
    }

    public bool MakeCultist(EntityUid uid, CultYoggRuleComponent component, bool initial = true)
    {
        //Grab the mind if it wasnt provided
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;

        _antagSelection.SendBriefing(uid, Loc.GetString("traitor-role-greeting"), null, component.GreetSoundNotification);

        component.CultistMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(uid, component.NanoTrasenFaction, false);
        _npcFaction.AddFaction(uid, component.CultYoggFaction);

        _entityManager.AddComponent<CultYoggComponent>(uid);
        _entityManager.AddComponent<ZombieImmuneComponent>(uid);//they are practically mushrooms

        //ToDo Give list of sacrificial

        return true;
    }

    protected override void AppendRoundEndText(EntityUid uid, CultYoggRuleComponent component, GameRuleComponent gameRule,
    ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (component.Summoned)
        {
            args.AddLine(Loc.GetString("cult-yogg-round-end-amount-win"));
        }
        else
        {
            var fraction = GetCultistsFraction();
            if (fraction <= 0)
                args.AddLine(Loc.GetString("cult-yogg-round-end-amount-none"));
            else if (fraction <= 2)
                args.AddLine(Loc.GetString("cult-yogg-round-end-amount-low"));
            else if (fraction < 12)
                args.AddLine(Loc.GetString("cult-yogg-round-end-amount-medium"));
            else
                args.AddLine(Loc.GetString("cult-yogg-round-end-amount-high"));
        }

        args.AddLine(Loc.GetString("cult-yogg-round-end-initial-count", ("initialCount", component.InitialCultistsNames.Count)));

        var antags = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", antags.Count)));
        foreach (var (_, data, entName) in antags)
        {
            args.AddLine(Loc.GetString("cult-yogg-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }
    }
    private float GetCultistsFraction()//надо учесть МиГо
    {
        int cultistsCount = 0;
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, CultYoggComponent, MobStateComponent>();
        while (query.MoveNext(out _, out _, out _, out var mob))
        {
            if (mob.CurrentState == MobState.Dead)
                continue;
            cultistsCount++;
        }

        return cultistsCount;
    }
}
