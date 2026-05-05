// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.CultYogg.Altar;
using Content.Shared.SS220.CultYogg.Buildings;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.Rave;
using Content.Shared.SS220.CultYogg.Sacrificials;
using Content.Shared.Verbs;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.SS220.CultYogg.MiGo;

public abstract class SharedMiGoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMiGoErectSystem _miGoErectSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, ComponentStartup>(OnComponentStartup);

        // actions
        SubscribeLocalEvent<MiGoComponent, MiGoHealActionEvent>(MiGoHealAction);
        SubscribeLocalEvent<MiGoComponent, MiGoErectActionEvent>(MiGoErectAction);
        SubscribeLocalEvent<MiGoComponent, MiGoSacrificeActionEvent>(MiGoSacrificeAction);
        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementActionEvent>(OnMiGoEnslaveAction);

        SubscribeLocalEvent<MiGoComponent, BoundUIOpenedEvent>(OnBoundUIOpened);

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerb);

        SubscribeLocalEvent<MiGoComponent, ChangeCultYoggStageEvent>(OnUpdateStage);
    }

    protected virtual void OnComponentStartup(Entity<MiGoComponent> uid, ref ComponentStartup args)
    {
        _actions.AddAction(uid, ref uid.Comp.MiGoHealActionEntity, uid.Comp.MiGoHealAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoEnslavementActionEntity, uid.Comp.MiGoEnslavementAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoErectActionEntity, uid.Comp.MiGoErectAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoSacrificeActionEntity, uid.Comp.MiGoSacrificeAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoToggleLightActionEntity, uid.Comp.MiGoToggleLightAction);

        SyncStage(uid);
    }

    protected virtual void SyncStage(Entity<MiGoComponent> uid) { }

    private void OnBoundUIOpened(Entity<MiGoComponent> entity, ref BoundUIOpenedEvent args)
    {
        switch (args.UiKey.ToString())
        {
            case "Erect":
                _userInterfaceSystem.SetUiState(args.Entity, args.UiKey, new MiGoErectBuiState()
                {
                    Buildings = _proto.GetInstances<CultYoggBuildingPrototype>()
                        .Values.Select(proto => (ProtoId<CultYoggBuildingPrototype>) proto)
                        .ToList(),
                });
                break;

            case "Plant":
                _userInterfaceSystem.SetUiState(args.Entity, args.UiKey, new MiGoPlantBuiState()
                {
                    Seeds = _proto.GetInstances<CultYoggSeedsPrototype>()
                        .Values.Select(proto => (ProtoId<CultYoggSeedsPrototype>) proto)
                        .ToList(),
                });
                break;
        }
    }

    private void OnGetVerb(GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess ||
            args.User == args.Target)
            return;

        // Enslave verb
        // ToDo for a future verb
        /*
        if (TryComp<MiGoComponent>(args.User, out var miGoComp) && miGoComp.IsPhysicalForm)
        {
            var enslaveVerb = new Verb
            {
                Text = Loc.GetString("cult-yogg-enslave-verb"),
                Icon = new SpriteSpecifier.Rsi(new ResPath("SS220/Interface/Actions/cult_yogg.rsi"), "enslavement"),
                Act = () =>
                {
                    if (!CanEnslaveTarget((args.User, miGoComp), args.Target, out var reason))
                    {
                        _popup.PopupPredicted(reason, args.Target, args.User);
                        return;
                    }

                    StartEnslaveDoAfter((args.User, miGoComp), args.Target);
                }
            };

            var healVerb = new Verb
            {
                Text = Loc.GetString("cult-yogg-heal-verb"),
                Icon = new SpriteSpecifier.Rsi(new ResPath("SS220/Interface/Actions/cult_yogg.rsi"), "heal"),
                Act = () =>
                {

                    //MiGoHeal((args.User, miGoComp), args.Target);
                }
            };

            args.Verbs.Add(enslaveVerb);
            args.Verbs.Add(healVerb);
        }
        */
    }

    #region Heal
    private void MiGoHealAction(Entity<MiGoComponent> uid, ref MiGoHealActionEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<MobStateComponent>(args.Target) || HasComp<BorgChassisComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-cant-heal-this", ("target", args.Target)), args.Target, uid);
            return;
        }


        var healComponent = EnsureComp<CultYoggHealComponent>(args.Target);

        healComponent.HealingEffectTime = _timing.CurTime + uid.Comp.HealingEffectTime;
        healComponent.Heal = args.Heal;
        healComponent.BloodlossModifier = args.BloodlossModifier;
        healComponent.ModifyBloodLevel = args.ModifyBloodLevel;
        healComponent.TimeBetweenHealingTicks = args.TimeBetweenIncidents;
        healComponent.Sprite = args.EffectSprite;
        healComponent.ModifyStamina = args.ModifyStamina;

        Dirty(args.Target, healComponent);

        args.Handled = true;
    }
    #endregion

    #region Erect
    private void MiGoErectAction(Entity<MiGoComponent> entity, ref MiGoErectActionEvent args)
    {
        //will wait when sw will update ui parts to copy paste, cause rn it has an errors
        if (args.Handled || !TryComp<ActorComponent>(entity, out var actor))
            return;

        _miGoErectSystem.OpenUI(entity, actor);
    }
    #endregion

    #region MiGoSacrifice
    private void MiGoSacrificeAction(Entity<MiGoComponent> uid, ref MiGoSacrificeActionEvent args)
    {
        if (uid.Comp.CurrentStage < CultYoggStage.Alarm)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-sacrifice-only-stage-alarm"), uid);
            return;
        }

        var altarsClose = _entityLookup.GetEntitiesInRange<CultYoggAltarComponent>(Transform(uid).Coordinates, uid.Comp.SacrificeStartRange);

        if (altarsClose.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-sacrifice-no-altars"), uid, uid);
            return;
        }

        foreach (var altar in altarsClose)
        {
            if (!TryComp<StrapComponent>(altar, out var strapComp))
                continue;

            if (strapComp.BuckledEntities.Count == 0)
                continue;

            if (!HasComp<CultYoggSacrificialComponent>(strapComp.BuckledEntities.First()))
                continue;

            TryDoSacrifice(altar, uid);
        }
    }

    private bool TryDoSacrifice(Entity<CultYoggAltarComponent> ent, EntityUid user)
    {
        if (!TryComp<StrapComponent>(ent, out var strapComp))
            return false;

        var targetUid = strapComp.BuckledEntities.FirstOrNull();

        if (targetUid == null)
            return false;

        var sacrificeDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.RitualTime, new MiGoSacrificeDoAfterEvent(), ent, ent)
        {
            BreakOnDamage = false,
            BreakOnMove = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            DistanceThreshold = 2.5f,
            MovementThreshold = 2.5f
        };

        var started = _doAfter.TryStartDoAfter(sacrificeDoAfter);

        if (started)
        {
            _popup.PopupPredicted(Loc.GetString("cult-yogg-sacrifice-started", ("user", user), ("target", targetUid)),
                ent, null, PopupType.MediumCaution);

            ent.Comp.AnnounceTime = _timing.CurTime + ent.Comp.AnnounceDelay;
        }

        return started;
    }

    #endregion

    #region Enslave
    private void OnMiGoEnslaveAction(Entity<MiGoComponent> ent, ref MiGoEnslavementActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (!CanEnslaveTarget(ent, target, out var reason))
        {
            _popup.PopupClient(reason, target, ent);
            _adminLogger.Add(LogType.Action, $"MiGo {ToPrettyString(ent):user} failed to enslave {ToPrettyString(target):target} because \"{reason}\"");
            return;
        }

        StartEnslaveDoAfter(ent, target);
        args.Handled = true;

        _adminLogger.Add(LogType.Action, $"MiGo {ToPrettyString(ent):user} successfully enslaved {ToPrettyString(target):target}");
    }

    protected void StartEnslaveDoAfter(Entity<MiGoComponent> entity, EntityUid target)
    {
        var (uid, comp) = entity;

        var doafterArgs = new DoAfterArgs(EntityManager, uid, comp.EnslaveTime, new MiGoEnslaveDoAfterEvent(), uid, target)//ToDo estimate time for Enslave
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = false,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(doafterArgs);
        _audio.PlayPredicted(comp.EnslavingSound, target, target);
    }

    protected bool CanEnslaveTarget(Entity<MiGoComponent> ent, EntityUid target, out string? reason)
    {
        reason = null;

        if (!HasComp<HumanoidProfileComponent>(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-must-be-human");
            return false;
        }

        if (!_mobState.IsAlive(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-must-be-alive");
            return false;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-mindshield");
            return false;
        }

        if (HasComp<RevolutionaryComponent>(target) || HasComp<ZombieComponent>(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-another-fraction");
            return false;
        }

        if (!HasComp<RaveComponent>(target) && AnyCultistsAlive())//If the mushroom was eaten or no cultists alive
        {
            reason = Loc.GetString("cult-yogg-enslave-should-eat-shroom");
            return false;
        }

        if (HasComp<CultYoggSacrificialComponent>(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-is-sacrificial");
            return false;
        }

        if (_mind.TryGetMind(target, out var mindId, out _))
        {
            if (TryComp<MindRoleComponent>(mindId, out var role) &&
                role.JobPrototype is { } job && job == "Chaplain")
            {
                reason = "cult-yogg-enslave-cant-be-a-chaplain";
                return false;
            }
        }
        else
        {
            if (_net.IsServer) // ToDo delete this check after MindContainer fixes
                reason = Loc.GetString("cult-yogg-no-mind");
            return false;
        }

        return true;
    }

    protected bool AnyCultistsAlive()
    {
        var queryCultists = EntityQueryEnumerator<CultYoggComponent>();
        while (queryCultists.MoveNext(out var ent, out _))
        {
            if (!_mobState.IsAlive(ent))
                continue;

            if (!_mind.TryGetMind(ent, out _, out _))
                continue;

            return true;
        }

        return false;
    }
    #endregion

    private void OnUpdateStage(Entity<MiGoComponent> ent, ref ChangeCultYoggStageEvent args)
    {
        if (ent.Comp.CurrentStage == args.Stage)
            return;

        ent.Comp.CurrentStage = args.Stage;
        Dirty(ent);
    }
}
