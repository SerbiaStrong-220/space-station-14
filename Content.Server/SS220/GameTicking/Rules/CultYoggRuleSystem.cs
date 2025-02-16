// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Server.SS220.CultYogg.Sacraficials;
using Content.Server.RoundEnd;
using Content.Server.Zombies;
using Content.Shared.Audio;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.CultYogg.Sacraficials;
using Content.Shared.SS220.StuckOnEquip;
using Content.Shared.SS220.Roles;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Database;
using Content.Server.Administration.Logs;
using Content.Server.SS220.Objectives.Systems;
using Content.Server.SS220.Objectives.Components;
using Robust.Shared.Utility;
using Content.Server.Pinpointer;
using Content.Server.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.AlertLevel;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Content.Server.EUI;
using Robust.Server.Player;
using Content.Server.SS220.CultYogg.DeCultReminder;

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class CultYoggRuleSystem : GameRuleSystem<CultYoggRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private List<List<string>> _sacraficialTiers = [];
    public TimeSpan DefaultShuttleArriving { get; set; } = TimeSpan.FromSeconds(85);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<CultYoggEnslavedEvent>(MiGoEnslave);
        SubscribeLocalEvent<CultYoggDeCultingEvent>(DeCult);

        SubscribeLocalEvent<SacraficialReplacementEvent>(SacraficialReplacement);

        SubscribeLocalEvent<CultYoggRuleComponent, CultYoggSacrificedTargetEvent>(OnTargetSacrificed);

        SubscribeLocalEvent<CultYoggAnouncementEvent>(SendCultAnounce);
    }

    #region Sacreficials picking
    /// <summary>
    /// Used to generate sacraficials at the start of the gamerule
    /// </summary>
    protected override void Started(EntityUid uid, CultYoggRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        if (component.SacraficialsWerePicked)
        {
            _adminLogger.Add(LogType.EventRan, LogImpact.High, $"CultYogg tried to tun several instanses of a gamurule");
            return;
        }

        component.SacraficialsWerePicked = true;//had wierd thing with multiple event calling, so i did this shit

        GenerateJobsList(component);
        //_adminLogger.Add(LogType.EventRan, LogImpact.High, $"CultYogg game rule has started picking up sacraficials");
        SetSacraficials(component);

        var ev = new CultYoggReinitObjEvent();
        var query = EntityQueryEnumerator<CultYoggSummonConditionComponent>();
        while (query.MoveNext(out var ent, out var _))
        {
            RaiseLocalEvent(ent, ref ev); //Reinitialise objective if gamerule was forced
        }
    }

    //Filling list of jobs fot better range
    private void GenerateJobsList(CultYoggRuleComponent comp)
    {
        if (_sacraficialTiers.Count != 0)
        {
            //_adminLogger.Add(LogType.EventRan, LogImpact.High, $"CultYogg tried to generate another tier list");
            return;
        }

        List<string> firstTier = comp.FirstTierJobs;//just captain as main target

        List<string> secondTier = [];//heads

        if (!_proto.TryIndex<DepartmentPrototype>(comp.SecondTierDepartament, out var commandList))
            return;

        foreach (var role in commandList.Roles)
        {
            if (firstTier.Contains(role.Id))
                continue;

            secondTier.Add(role.Id);
        }

        List<string> thirdTier = [];//everybody else except heads

        foreach (var departament in _proto.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (comp.BannedDepartaents.Contains(departament.ID))
                continue;

            if (departament.ID == comp.SecondTierDepartament)
                continue;

            foreach (ProtoId<JobPrototype> role in departament.Roles)
            {
                if (firstTier.Contains(role.Id))
                    continue;

                if (secondTier.Contains(role.Id))
                    continue;

                thirdTier.Add(role.Id);
            }
        }

        _sacraficialTiers.Add(firstTier);
        _sacraficialTiers.Add(secondTier);
        _sacraficialTiers.Add(thirdTier);
    }

    private void SetSacraficials(CultYoggRuleComponent component)
    {
        var allHumans = GetAliveNoneCultHumans();

        if (allHumans is null)
            return;

        _adminLogger.Add(LogType.EventRan, LogImpact.High, $"Amount of tiers is {_sacraficialTiers.Count}");
        for (int i = 0; i < _sacraficialTiers.Count; i++)
        {
            //_adminLogger.Add(LogType.EventRan, LogImpact.High, $"CultYogg trying to pick {i} tier, max tiers {_sacraficialTiers.Count}");
            SetSacraficeTarget(component, PickFromTierPerson(allHumans, i), i);
        }
    }

    public EntityUid? PickFromTierPerson(List<EntityUid> allHumans, int tier)//ToDo wierd naming
    {
        if (tier >= _sacraficialTiers.Count)
        {
            //_adminLogger.Add(LogType.EventRan, LogImpact.High, $"CultYogg tier: {tier} is over amount of tiers {_sacraficialTiers.Count}. Exiting the loop");
            return null;
        }

        var allSuitable = new List<EntityUid>();

        foreach (var mind in allHumans)
        {
            // RequireAdminNotify used as a cheap way to check for command department
            if (!_job.MindTryGetJob(mind, out var prototype))
                continue;

            if (_sacraficialTiers[tier].Contains(prototype.ID))
                allSuitable.Add(mind);
        }

        if (allSuitable.Count == 0)
        {
            //_adminLogger.Add(LogType.EventRan, LogImpact.High, $"CultYogg tier: {tier}, has no suitable people trying to pick next tier, max {_sacraficialTiers.Count}");
            return PickFromTierPerson(allHumans, ++tier);
        }

        return _random.Pick(allSuitable);
    }

    private void SetSacraficeTarget(CultYoggRuleComponent component, EntityUid? uid, int tier)
    {
        if (uid is null)
            return;

        if (!TryComp<MindComponent>(uid, out var mind))
            return;

        if (mind.Session is null)
            return;

        if (mind.Session.AttachedEntity is null)
            return;

        //_adminLogger.Add(LogType.EventRan, LogImpact.High, $"CultYogg person {meta.EntityName} where picked for a tier: {tier}");

        var sacrComp = EnsureComp<CultYoggSacrificialComponent>(mind.Session.AttachedEntity.Value);

        sacrComp.Tier = tier;
    }

    private List<EntityUid> GetAliveNoneCultHumans()//maybe add here sacraficials and cultists filter
    {
        var mindQuery = EntityQuery<MindComponent>();

        var allHumans = new List<EntityUid>();
        // HumanoidAppearanceComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out var mc, out var mobState, out _))
        {
            // the player needs to have a mind and not be the excluded one
            if (mc.Mind == null)
                continue;

            if (HasComp<CultYoggComponent>(uid))
                continue;

            if (HasComp<CultYoggSacrificialComponent>(uid))
                continue;

            // the player has to be alive
            if (_mobState.IsAlive(uid, mobState))
                allHumans.Add(mc.Mind.Value);
        }

        return allHumans;
    }
    #endregion

    #region Sacraficials Events
    private void OnTargetSacrificed(Entity<CultYoggRuleComponent> entity, ref CultYoggSacrificedTargetEvent args)
    {
        var (_, comp) = entity;
        var (altar, altarComp) = args.Altar;
        comp.AmountOfSacrifices++;

        if (comp.AmountOfSacrifices == comp.AmountOfSacrificesToWarningAnouncement)
        {
            foreach (var station in _station.GetStations())
            {
                _chat.DispatchStationAnnouncement(station, Loc.GetString("cult-yogg-cultists-warning"), playSound: false, colorOverride: Color.Red);
                _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
                _alertLevel.SetLevel(station, "gamma", true, true, true);
            }
        }

        if (comp.AmountOfSacrifices == comp.AmountOfSacrificesToGodSummon)
            SummonGod(entity, Transform(altar).Coordinates);
    }

    private void SacraficialReplacement(ref SacraficialReplacementEvent args)
    {
        var cultRuleComp = GetCultGameRule();

        if (cultRuleComp == null)
            return;

        if (!TryComp<CultYoggSacrificialComponent>(args.Entity, out var sacrComp))
            return;

        if (sacrComp.WasSacraficed)
            return;

        SetNewSacraficial(cultRuleComp, sacrComp.Tier);

        RemComp<CultYoggSacrificialComponent>(args.Entity);

        var ev = new CultYoggAnouncementEvent(args.Entity, Loc.GetString("cult-yogg-sacraficial-was-replaced", ("name", MetaData(args.Entity).EntityName)));
        RaiseLocalEvent(args.Entity, ref ev, true);
    }
    private void SetNewSacraficial(CultYoggRuleComponent comp, int tier)
    {
        var allHumans = GetAliveNoneCultHumans();

        if (allHumans is null)
            return;

        SetSacraficeTarget(comp, PickFromTierPerson(allHumans, tier), tier);
    }
    #endregion

    #region Enslaving
    /// <summary>
    /// If MiGo enslaves somebody -- will call this
    /// </summary>
    /// <param name="args.Target">Target of enslavement</param>
    private void MiGoEnslave(ref CultYoggEnslavedEvent args)
    {
        if (args.Target == null)
            return;

        var cultRuleComp = GetCultGameRule();

        if (cultRuleComp == null)
            return;

        MakeCultist(args.Target.Value, cultRuleComp, false);
    }
    #endregion

    #region Cultists making
    private void AfterEntitySelected(Entity<CultYoggRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, ent);
    }

    public void MakeCultist(EntityUid uid, CultYoggRuleComponent comp, bool initial = true)
    {
        //Grab the mind if it wasnt provided
        if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
            return;

        _antag.SendBriefing(uid, Loc.GetString("cult-yogg-role-greeting"), null, comp.GreetSoundNotification);

        if (initial)
            comp.InitialCultistMinds.Add(mindId);

        // Change the faction
        _npcFaction.RemoveFaction(uid, comp.NanoTrasenFaction, false);
        _npcFaction.AddFaction(uid, comp.CultYoggFaction);

        EnsureComp<CultYoggComponent>(uid);

        //update stage cause it might be midstage
        var ev = new ChangeCultYoggStageEvent(comp.AmountOfSacrifices);
        RaiseLocalEvent(uid, ref ev);

        //Add telepathy
        var telepathy = EnsureComp<TelepathyComponent>(uid);
        telepathy.CanSend = true;//we are allowing it cause testing
        telepathy.TelepathyChannelPrototype = comp.TelepathyChannel;

        EnsureComp<ShowCultYoggIconsComponent>(uid);//icons of cultists and sacraficials
        EnsureComp<ZombieImmuneComponent>(uid);//they are practically mushrooms

        foreach (var obj in comp.ListofObjectives)
        {
            _role.MindAddRole(mindId, comp.MindCultYoggAntagId, mindComp, true);
            var objective = _mind.TryAddObjective(mindId, mindComp, obj);
        }
    }
    #endregion

    #region Cultists de-making

    private void DeCult(ref CultYoggDeCultingEvent args)
    {
        var cultRuleComp = GetCultGameRule();//ToDo bug potentialy if somebody will make cultist without gamerule, ask head dev

        if (cultRuleComp == null)
            return;

        DeMakeCultist(args.Entity, cultRuleComp);
    }

    public void DeMakeCultist(EntityUid uid, CultYoggRuleComponent component)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
            return;

        if (!_role.MindHasRole<CultYoggRoleComponent>(mindId, out var cultRoleEnt))
            return;

        foreach (var obj in component.ListofObjectives)
        {
            if (!_mind.TryFindObjective(mindId, obj, out var objUid))
                continue;

            _mind.TryRemoveObjective(mindId, mindComp, objUid.Value);
        }

        _role.MindRemoveRole<CultYoggRoleComponent>(mindId);

        //Remove all corrupted items
        var ev = new DropAllStuckOnEquipEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);

        // Change the faction
        _npcFaction.RemoveFaction(uid, component.CultYoggFaction, false);
        _npcFaction.AddFaction(uid, component.NanoTrasenFaction);

        //Remove telepathy
        RemComp<TelepathyComponent>(uid);

        RemComp<ShowCultYoggIconsComponent>(uid);
        RemComp<ZombieImmuneComponent>(uid);

        if (mindComp.UserId != null &&
            _playerManager.TryGetSessionById(mindComp.UserId.Value, out var session))
        {
            _euiManager.OpenEui(new DeCultReminderEui(), session);
        }
    }
    #endregion

    #region Anounce
    private void SendCultAnounce(ref CultYoggAnouncementEvent args)
    {
        //ToDo refactor without spam

        var ruleComp = GetCultGameRule();

        if (ruleComp == null)
            return;

        var ev = new TelepathyAnnouncementSendEvent(args.Message, ruleComp.TelepathyChannel);
        RaiseLocalEvent(args.Entity, ev, true);
    }
    #endregion

    #region RoundEnding
    private void SummonGod(Entity<CultYoggRuleComponent> entity, EntityCoordinates coordinates)
    {
        var (_, comp) = entity;
        var godUid = Spawn(comp.GodPrototype, coordinates);

        foreach (var station in _station.GetStations())
        {
            _chat.DispatchStationAnnouncement(station, Loc.GetString("cult-yogg-shuttle-call", ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(godUid)))), colorOverride: Color.Crimson);
            _alertLevel.SetLevel(station, "delta", true, true, true);
        }
        _roundEnd.RequestRoundEnd(DefaultShuttleArriving, null);

        var selectedSong = _audio.GetSound(comp.SummonMusic);

        if (!string.IsNullOrEmpty(selectedSong))
            _sound.DispatchStationEventMusic(godUid, selectedSong, StationEventMusicType.Nuke);//should i rename somehow?

        comp.Summoned = true;//Win EndText
    }
    #endregion

    #region EndText
    /// <summary>
    /// EndText copypasted from zombies. Hasn't finished.
    /// </summary>
    protected override void AppendRoundEndText(EntityUid uid, CultYoggRuleComponent component, GameRuleComponent gameRule,
    ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (component.Summoned)
        {
            args.AddLine(Loc.GetString("cult-yogg-round-end-win"));
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

        args.AddLine(Loc.GetString("cult-yogg-round-end-initial-count", ("initialCount", component.InitialCultistMinds.Count)));

        var antags = _antag.GetAntagIdentifiers(uid);
        //args.AddLine(Loc.GetString("zombie-round-end-initial-count", ("initialCount", antags.Count))); // ToDo Should we add this?
        foreach (var (mind, data, entName) in antags)
        {
            if (!component.InitialCultistMinds.Contains(mind))
                continue;

            args.AddLine(Loc.GetString("cult-yogg-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }
    }
    private float GetCultistsFraction()
    {
        int cultistsCount = 0;
        var queryCultists = EntityQueryEnumerator<HumanoidAppearanceComponent, CultYoggComponent, MobStateComponent>();
        while (queryCultists.MoveNext(out _, out _, out _, out var mob))
        {
            if (mob.CurrentState == MobState.Dead)
                continue;
            cultistsCount++;
        }

        var queryMiGo = EntityQueryEnumerator<MiGoComponent, MobStateComponent>();
        while (queryMiGo.MoveNext(out _, out _, out var mob))
        {
            if (mob.CurrentState == MobState.Dead)
                continue;
            cultistsCount++;
        }

        return cultistsCount;
    }
    #endregion

    public CultYoggRuleComponent? GetCultGameRule()
    {
        CultYoggRuleComponent? comp = null;

        var query = QueryAllRules();
        while (query.MoveNext(out _, out var cultComp, out _))
        {
            comp = cultComp;
        }

        return comp;
    }
}

/// <summary>
///     Raised when we need announce smth to all cultists and we dont have their channel
/// </summary>
[ByRefEvent, Serializable]
public sealed class CultYoggAnouncementEvent : EntityEventArgs
{
    public readonly EntityUid Entity;
    public readonly string Message;

    public CultYoggAnouncementEvent(EntityUid entity, string message)
    {
        Entity = entity;
        Message = message;
    }
}
