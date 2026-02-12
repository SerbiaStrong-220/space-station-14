// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Audio;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Pinpointer;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.SS220.CultYogg.DeCultReminder;
using Content.Server.SS220.CultYogg.Sacrificials;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.CultYogg.Altar;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.CultYogg.Sacrificials;
using Content.Shared.SS220.InnerHandToggleable;
using Content.Shared.SS220.RestrictedItem;
using Content.Shared.SS220.Roles;
using Content.Shared.SS220.StuckOnEquip;
using Content.Shared.SS220.Telepathy;
using Content.Shared.Station.Components;
using Content.Shared.Zombies;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class CultYoggRuleSystem : GameRuleSystem<CultYoggRuleComponent>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
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
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedRestrictedItemSystem _sharedRestrictedItemSystem = default!;
    [Dependency] private readonly SharedStuckOnEquipSystem _stuckOnEquip = default!;
    [Dependency] private readonly SharedMiGoSystem _migo = default!;

    public TimeSpan DefaultShuttleArriving { get; set; } = TimeSpan.FromSeconds(85);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<CultYoggEnslavedEvent>(MiGoEnslave);
        SubscribeLocalEvent<CultYoggDeCultingEvent>(DeCult);

        SubscribeLocalEvent<SacrificialReplacementEvent>(SacrificialReplacement);

        SubscribeLocalEvent<CultYoggRuleComponent, CultYoggSacrificedTargetEvent>(OnTargetSacrificed);
        SubscribeLocalEvent<CultYoggRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<ProgressCultEvent>(OnProgressCult);
    }

    protected override void Added(EntityUid uid, CultYoggRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.InitialCrewCount = GameTicker.ReadyPlayerCount();
        GenerateStagesCount((uid, component));//trying to count cultists, but someone "made a really questionable balance change"
    }

    #region Sacrificials picking
    private bool TrySetSacrificial(CultYoggRuleComponent comp)
    {
        var allSuitable = GetAllSuitable();

        EntityUid? sacrificial;

        if (!TryPickCommandSacrificial(comp, allSuitable, out sacrificial))
            Log.Error($"CultYogg failed to pick command sacrificial");
        else
        {
            SetSacrificeTarget(sacrificial.Value);
            return true;
        }

        if (!TryPickAnySacrificial(comp, allSuitable, out sacrificial))
            Log.Error($"CultYogg failed to pick any non cultist alive sacrificial");
        else
        {
            SetSacrificeTarget(sacrificial.Value);
            return true;
        }
        _chatManager.SendAdminAlert(Loc.GetString("CultYogg failed to pick any non cultist alive sacrificial on station, Game rule needs a manual admin picking"));
        return false;
    }

    public bool TryPickCommandSacrificial(CultYoggRuleComponent comp, List<EntityUid> allHumans, [NotNullWhen(true)] out EntityUid? sacrificial)
    {
        sacrificial = null;

        var allSuitable = new List<EntityUid>();

        if (!_proto.TryIndex(comp.SacrificialDepartament, out var sacrificialDepartament))
            return false;

        foreach (var mind in allHumans)
        {
            if (!_job.MindTryGetJob(mind, out var jobProto))
                continue;

            if (sacrificialDepartament.Roles.Contains(jobProto.ID))
                allSuitable.Add(mind);
        }

        if (allSuitable.Count <= 0)
            return false;

        sacrificial = _random.Pick(allSuitable);

        if (sacrificial != null)
            return true;

        return false;
    }

    public bool TryPickAnySacrificial(CultYoggRuleComponent comp, List<EntityUid> allHumans, [NotNullWhen(true)] out EntityUid? sacrificial)
    {
        sacrificial = null;

        if (allHumans.Count <= 0)
            return false;

        sacrificial = _random.Pick(allHumans);

        if (sacrificial != null)
            return true;

        return false;
    }

    private void SetSacrificeTarget(EntityUid uid)
    {
        if (!TryComp<MindComponent>(uid, out var mind))
            return;

        if (!_playerManager.TryGetSessionById(mind.UserId, out var session))
            return;

        if (session.AttachedEntity is null)
            return;

        EnsureComp<CultYoggSacrificialComponent>(session.AttachedEntity.Value);
    }

    private List<EntityUid> GetAllSuitable()
    {
        var allHumans = new List<EntityUid>();

        if (!TryGetRandomStation(out var station))//IDK how to get station so i took this realization
            return allHumans;

        // HumanoidAppearanceComponent is used to prevent mice, pAIs, etc from being chosen
        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, HumanoidAppearanceComponent>();
        while (query.MoveNext(out var uid, out var mc, out var mobState, out _))
        {
            // the player needs to have a mind and not be the excluded one
            if (mc.Mind == null)
                continue;

            if (HasComp<CultYoggSacrificialComponent>(uid))
                continue;

            if (HasComp<CultYoggComponent>(uid))
                continue;

            if (_station.GetOwningStation(uid) != station)
                continue;

            // the player has to be alive
            if (_mobState.IsAlive(uid, mobState))
                allHumans.Add(mc.Mind.Value);
        }

        return allHumans;
    }
    #endregion

    #region Sacrificials Events
    private void OnTargetSacrificed(Entity<CultYoggRuleComponent> rule, ref CultYoggSacrificedTargetEvent args)
    {
        rule.Comp.LastSacrificialAltar = args.Altar;
        rule.Comp.AmountOfSacrifices++;

        while (TryGetNextStage(rule, out var nextStage, out var nextStageDefinition)
            && nextStageDefinition.SacrificesRequired is { } sacrificesRequired
            && sacrificesRequired <= rule.Comp.AmountOfSacrifices)//ToDo_SS220 make it a bit better
        {
            ProgressToStage(rule, nextStage, nextStageDefinition);
        }
    }

    private void SacrificialReplacement(ref SacrificialReplacementEvent args)
    {
        if (!TryGetCultGameRule(out var rule))
            return;

        if (!TryComp<CultYoggSacrificialComponent>(args.Entity, out var sacrComp))
            return;

        if (sacrComp.WasSacrificed)
            return;

        TrySetSacrificial(rule.Value.Comp);

        RemComp<CultYoggSacrificialComponent>(args.Entity);

        SendCultAnounce(Loc.GetString("cult-yogg-sacrificial-was-replaced", ("name", MetaData(args.Entity).EntityName)));
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

        if (!TryGetCultGameRule(out var rule))
            return;

        if (!TryMakeCultistMind(args.Target.Value, rule.Value, false))
            return;

        MakeCultist(args.Target.Value, rule.Value);
    }
    #endregion

    #region Cultists making
    private void AfterEntitySelected(Entity<CultYoggRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!TryMakeCultistMind(args.EntityUid, ent, true))
            return;

        MakeCultist(args.EntityUid, ent);
        UpdateMiGoTeleportList();
    }

    /// <summary>
    /// The separation is made for better operation of the cloner and from potential other problems.
    /// </summary>
    /// <param name="initial">Flag for appearing in post-match credits</param>
    /// <param name="sendBriefing">Should there be sounds and briefing?</param>
    public bool TryMakeCultistMind(EntityUid uid, Entity<CultYoggRuleComponent> rule, bool initial = false, bool sendBriefing = true)
    {
        //Grab the mind if it wasnt provided
        if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
            return false;

        if (sendBriefing)
            _antag.SendBriefing(uid, Loc.GetString("cult-yogg-role-greeting"), null, rule.Comp.GreetSoundNotification);

        if (initial && !rule.Comp.InitialCultistMinds.Contains(mindId))
            rule.Comp.InitialCultistMinds.Add(mindId);

        _role.MindAddRole(mindId, rule.Comp.MindCultYoggAntagId, mindComp, true);

        GiveAllActiveObjectives(rule, mindId, mindComp);

        rule.Comp.TotalCultistsConverted++;

        DirtyEntity(mindId);//Not quite sure if it is required

        return true;
    }

    public void MakeCultist(EntityUid uid, Entity<CultYoggRuleComponent> rule)
    {
        // Change the faction
        _npcFaction.RemoveFaction(uid, rule.Comp.NanoTrasenFaction, false);
        _npcFaction.AddFaction(uid, rule.Comp.CultYoggFaction);

        EnsureComp<CultYoggComponent>(uid);

        //update stage cause it might be midstage
        var ev = new ChangeCultYoggStageEvent(rule.Comp.Stage);//ToDo_SS220 make it function
        RaiseLocalEvent(uid, ref ev);

        //Add telepathy
        var telepathy = EnsureComp<TelepathyComponent>(uid);
        telepathy.CanSend = true;//we are allowing it cause testing
        telepathy.TelepathyChannelPrototype = rule.Comp.TelepathyChannel;

        //allows to hide the sedative sting
        var innerToggle = EnsureComp<InnerHandToggleableComponent>(uid);
        innerToggle.Whitelist = rule.Comp.WhitelistToggleable;

        EnsureComp<ShowCultYoggIconsComponent>(uid);//icons of cultists and sacrificials
        EnsureComp<ZombieImmuneComponent>(uid);//they are practically mushrooms

        var cultifiedEv = new GotCultifiedEvent();
        RaiseLocalEvent(uid, ref cultifiedEv);

        DirtyEntity(uid);
    }
    #endregion

    #region Cultists de-making
    private void DeCult(ref CultYoggDeCultingEvent args)
    {
        if (!TryGetCultGameRule(out var rule))
            return;

        DeCultMind(args.Entity, rule.Value);

        DeMakeCultist(args.Entity, rule.Value.Comp);

        UpdateMiGoTeleportList();
    }

    public void DeCultMind(EntityUid uid, Entity<CultYoggRuleComponent> rule)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
            return;

        if (!_role.MindHasRole<CultYoggRoleComponent>(mindId, out var _))
            return;

        RemoveAllActiveObjectives(rule, mindId, mindComp);

        _role.MindRemoveRole<CultYoggRoleComponent>(mindId);

        if (mindComp.UserId != null &&
            _playerManager.TryGetSessionById(mindComp.UserId.Value, out var session))
        {
            _euiManager.OpenEui(new DeCultReminderEui(), session);
        }

        DirtyEntity(mindId);//Not quite sure if it is required
    }

    public void DeMakeCultist(EntityUid uid, CultYoggRuleComponent component)
    {
        //Remove all corrupted items
        _stuckOnEquip.RemoveAllStuckItems(uid);

        _sharedRestrictedItemSystem.DropAllRestrictedItems(uid);//ToDo_SS220 make it by component or tag

        // Change the faction
        _npcFaction.RemoveFaction(uid, component.CultYoggFaction, false);
        _npcFaction.AddFaction(uid, component.NanoTrasenFaction);

        //remove cultist component
        RemComp<CultYoggComponent>(uid);

        //Remove telepathy
        RemComp<TelepathyComponent>(uid);

        RemComp<InnerHandToggleableComponent>(uid);

        RemComp<ShowCultYoggIconsComponent>(uid);
        RemComp<ZombieImmuneComponent>(uid);

        DirtyEntity(uid);
    }
    #endregion

    #region Anounce
    public void SendCultAnounce(string message)
    {
        //ToDo refactor without spam

        if (!TryGetCultGameRule(out var rule))
            return;

        var ev = new TelepathyAnnouncementSendEvent(message, rule.Value.Comp.TelepathyChannel);
        RaiseLocalEvent(rule.Value, ev, true);
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

        var selectedSong = _audio.ResolveSound(comp.SummonMusic);

        _sound.DispatchStationEventMusic(godUid, selectedSong, StationEventMusicType.Nuke);//should i rename somehow?
    }

    private EntityCoordinates FindGodSummonCoordinates(Entity<CultYoggRuleComponent> rule)
    {
        if (rule.Comp.LastSacrificialAltar is { } lastAltar
            && Exists(lastAltar))
        {
            return Transform(lastAltar).Coordinates;
        }

        var queryAltar = EntityQueryEnumerator<CultYoggAltarComponent>();
        while (queryAltar.MoveNext(out var uid, out _))
        {
            return Transform(uid).Coordinates;
        }

        var queryMiGo = EntityQueryEnumerator<MiGoComponent>();
        while (queryMiGo.MoveNext(out var uid, out _))
        {
            return Transform(uid).Coordinates;
        }

        var queryCultists = EntityQueryEnumerator<CultYoggComponent>();
        while (queryCultists.MoveNext(out var uid, out _))
        {
            return Transform(uid).Coordinates;
        }

        foreach (var station in _station.GetStations())
        {
            if (_station.GetLargestGrid((station, Comp<StationDataComponent>(station))) is { } grid)
                return Transform(grid).Coordinates;
        }

        // At this point we are probably on the empty map so I don't know what to do.
        return EntityCoordinates.Invalid;
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

        if (component.Stage is CultYoggStage.God)
        {
            args.AddLine(Loc.GetString("cult-yogg-round-end-win"));
        }
        else
        {
            //ToDo_SS220 rework this migic numbers shit
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

        foreach (var (mind, data, entName) in antags)
        {
            if (!component.InitialCultistMinds.Contains(mind))
                continue;

            args.AddLine(Loc.GetString("cult-yogg-round-end-user-was-initial",
                ("name", entName),
                ("username", data.UserName)));
        }
    }
    #endregion

    #region Stages
    private void OnProgressCult(ref ProgressCultEvent args)
    {
        if (!TryGetCultGameRule(out var rule))
            return;

        var amountOfCultists = GetCultistsFraction();

        if (!TryGetNextStage(rule.Value, out var nextStage, out var nextStageDefinition))
            return;

        if (nextStageDefinition is null)
            return;

        if (nextStageDefinition.CultistsAmountRequired == null)
            return;

        if (nextStageDefinition.CultistsAmountRequired > amountOfCultists)
            return;

        ProgressToStage(rule.Value, nextStage, nextStageDefinition);
    }

    private void GenerateStagesCount(Entity<CultYoggRuleComponent> rule)
    {
        if (!TryComp<AntagSelectionComponent>(rule.Owner, out var selectionComp))
            return;

        var count = _antag.GetTargetAntagCount((rule, selectionComp), rule.Comp.InitialCrewCount);

        foreach (var (stage, stageDef) in rule.Comp.Stages)
        {
            if (stageDef.CultistsToCrewFraction is null)
                continue;

            stageDef.CultistsAmountRequired = count + (int)stage;

            int percentAmount = (int)(rule.Comp.InitialCrewCount * stageDef.CultistsToCrewFraction);

            if (percentAmount <= stageDef.CultistsAmountRequired)
                continue;

            stageDef.CultistsAmountRequired = percentAmount;
        }
    }

    private static bool TryGetNextStage(Entity<CultYoggRuleComponent> rule,
        out CultYoggStage nextStage, [NotNullWhen(true)] out CultYoggStageDefinition? stageDefinition)
    {
        nextStage = rule.Comp.Stage + 1;
        if (!rule.Comp.Stages.TryGetValue(nextStage, out stageDefinition))
            return false;

        return true;
    }

    private void ProgressToStage(Entity<CultYoggRuleComponent> rule, CultYoggStage stage, CultYoggStageDefinition stageDefinition)
    {
        // Only forward
        if (stage <= rule.Comp.Stage)
            return;

        rule.Comp.Stage = stage;

        _adminLogger.Add(LogType.RoundFlow, LogImpact.High, $"Cult Yogg progressed to {stage}");
        _chatManager.SendAdminAlert(Loc.GetString("cult-yogg-stage-admin-alert", ("stage", stage)));

        if (!TryGetNextStage(rule, out _, out var nextStageDefinition))
            return;

        //Adding Stage sacrificials for progressing for a next stage
        TryInitializeNextStageSacrificials(rule, nextStageDefinition);

        //doing stage non-entity-related things
        DoStageEffects(rule, stage);

        //Sending all cult related new stage and new stage objectives
        var changeStageEvent = new ChangeCultYoggStageEvent(stage);

        UpdateStageOnEnts<CultYoggComponent>(ref changeStageEvent, stageDefinition);
        UpdateStageOnEnts<MiGoComponent>(ref changeStageEvent, stageDefinition);
    }

    private void UpdateStageOnEnts<T>(ref ChangeCultYoggStageEvent stageEvent, CultYoggStageDefinition stageDef) where T : IComponent
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out _))
        {
            RaiseLocalEvent(uid, ref stageEvent);

            if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
                continue;

            TryGiveStageObjectives(mindId, mindComp, stageDef);
        }
    }

    private void DoStageEffects(Entity<CultYoggRuleComponent> rule, CultYoggStage stage)
    {
        switch (stage)
        {
            case CultYoggStage.Reveal:
                SendCultAnounce(Loc.GetString("cult-yogg-reveal-telepathy-announce"));
                break;
            case CultYoggStage.Alarm:
                SendCultAnounce(Loc.GetString("cult-yogg-alarm-telepathy-announce"));
                rule.Comp.AlertTime = _gameTiming.CurTime + rule.Comp.BeforeAlertTime;
                break;
            case CultYoggStage.God:
                SummonGod(rule, FindGodSummonCoordinates(rule));
                break;
            default:
                break;
        }
    }

    private bool TryInitializeNextStageSacrificials(Entity<CultYoggRuleComponent> rule, CultYoggStageDefinition stageDefinition)
    {
        if (stageDefinition.SacrificesRequired is null)
            return false;

        var sacrInitCount = stageDefinition.SacrificesRequired - rule.Comp.AmountOfSacrifices;

        for (var i = 0; i < sacrInitCount; i++)
        {
            TrySetSacrificial(rule.Comp);
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggRuleComponent>();

        while (query.MoveNext(out var _, out var rule))
        {
            if (rule.AlertTime is null)
                continue;

            if (_gameTiming.CurTime < rule.AlertTime)
                continue;

            foreach (var station in _station.GetStations())
            {
                _chat.DispatchStationAnnouncement(station, Loc.GetString("cult-yogg-cultists-warning"), announcementSound: rule.BroadcastSound, colorOverride: Color.Red);
                _alertLevel.SetLevel(station, "gamma", true, true, true);
            }

            rule.AlertTime = null;
        }
    }
    #endregion

    #region Objectives
    private void GiveAllActiveObjectives(Entity<CultYoggRuleComponent> rule, EntityUid mindId, MindComponent mindComp)
    {
        foreach (var (stage, stageDef) in rule.Comp.Stages)
        {
            if (stage > rule.Comp.Stage)
                continue;

            TryGiveStageObjectives(mindId, mindComp, stageDef);
        }
    }

    private bool TryGiveStageObjectives(EntityUid mindId, MindComponent mindComp, CultYoggStageDefinition stageDef)
    {
        if (stageDef.StageObjectives is null)
            return false;

        foreach (var obj in stageDef.StageObjectives)
        {
            _mind.TryAddObjective(mindId, mindComp, obj);
        }

        return true;
    }

    private void RemoveAllActiveObjectives(Entity<CultYoggRuleComponent> rule, EntityUid mindId, MindComponent mindComp)
    {
        foreach (var (stage, stageDef) in rule.Comp.Stages)
        {
            if (stage > rule.Comp.Stage)
                continue;

            TryRemoveStageObjectives(mindId, mindComp, stageDef);
        }
    }

    private bool TryRemoveStageObjectives(EntityUid mindId, MindComponent mindComp, CultYoggStageDefinition stageDef)
    {
        if (stageDef.StageObjectives is null)
            return false;

        foreach (var obj in stageDef.StageObjectives)
        {
            if (!_mind.TryFindObjective(mindId, obj, out var objUid))
                continue;

            _mind.TryRemoveObjective(mindId, mindComp, objUid.Value);
        }

        return true;
    }
    #endregion

    #region Briefing info
    private void OnGetBriefing(Entity<CultYoggRoleComponent> role, ref GetBriefingEvent args)
    {
        if (args.Briefing != null)
            return;

        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;

        if (!TryGetCultGameRule(out var rule))
            return;

        args.Append(MakeBriefing(rule.Value));
    }

    private string MakeBriefing(Entity<CultYoggRuleComponent> rule)
    {
        var briefing = Loc.GetString("cult-yogg-cultists-numb-info", ("aliveCultists", GetAliveCultistsNumber()), ("cultists", GetCultistsNumber()), ("aliveMiGo", GetAliveMiGoNumber()), ("MiGo", GetMiGoNumber()));

        if (TryGetNextStage(rule, out var nextStage, out var nextStageDefinition))
        {
            var count = "-";
            if (nextStageDefinition.CultistsAmountRequired != null)
                count = nextStageDefinition.CultistsAmountRequired.Value.ToString();//well not sure

            briefing += "\n" + Loc.GetString("cult-yogg-stage-info", ("stage", rule.Comp.Stage), ("count", count));
        }

        return briefing;
    }

    #endregion

    public bool TryGetCultGameRule([NotNullWhen(true)] out Entity<CultYoggRuleComponent>? rule)
    {
        rule = null;

        var query = QueryAllRules();
        while (query.MoveNext(out var uid, out var cultComp, out _))
        {
            rule = (uid, cultComp);
            return true;
        }

        return false;
    }

    public void UpdateMiGoTeleportList()//i made this cause idk any other ways to properly trigger this like PrototypesReloadedEventArgs
    {
        var queryMiGo = EntityQueryEnumerator<MiGoComponent>();

        while (queryMiGo.MoveNext(out var ent, out _))
        {
            _migo.UpdateTeleportTargets(ent);
        }
    }

    #region Cultist counting
    /// <summary>
    /// Getting the number of all Mi-Go and cultists.
    /// </summary>
    public int GetCultistsFraction()
    {
        return GetCultistsNumber() + GetMiGoNumber();
    }

    public int GetAliveCultistsNumber()
    {
        int cultistsCount = 0;
        var queryCultists = EntityQueryEnumerator<CultYoggComponent>();
        while (queryCultists.MoveNext(out var ent, out _))
        {
            if (!_mobState.IsAlive(ent))
                continue;

            if (!_mind.TryGetMind(ent, out _, out _))
                continue;

            cultistsCount++;
        }

        return cultistsCount;
    }

    public int GetCultistsNumber()
    {
        int cultistsCount = 0;
        var queryCultists = EntityQueryEnumerator<CultYoggComponent>();
        while (queryCultists.MoveNext(out _, out _))
        {
            cultistsCount++;
        }

        return cultistsCount;
    }

    public int GetAliveMiGoNumber()
    {
        int migoCount = 0;
        var queryCultists = EntityQueryEnumerator<MiGoComponent>();
        while (queryCultists.MoveNext(out var ent, out _))
        {
            if (!_mobState.IsAlive(ent))
                continue;

            if (!_mind.TryGetMind(ent, out _, out _))
                continue;

            migoCount++;
        }

        return migoCount;
    }

    public int GetMiGoNumber()
    {
        int migoCount = 0;
        var queryCultists = EntityQueryEnumerator<MiGoComponent>();
        while (queryCultists.MoveNext(out _, out _))
        {
            migoCount++;
        }

        return migoCount;
    }
    #endregion
}
