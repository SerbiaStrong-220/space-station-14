// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Ui;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] protected SurgeryGraphSystem SurgeryGraph = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ISharedAdminLogManager _adminLogManager = default!;
    [Dependency] private SharedMeleeWeaponSystem _meleeWeapon = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private const float ErrorGettingDelayDelay = 8f;
    private const float DoAfterMovementThreshold = 0.15f;
    private const int SurgeryExaminePushPriority = -1;

    private const SurgeryEdgeSelectorUi EdgeSelectorBUIKey = SurgeryEdgeSelectorUi.Key;

    private static readonly LocId SurgeryCantPerformOnYourself = "surgery-cant-perform-on-yourself";
    private static readonly LocId SurgeryCancelledOnStart = "surgery-cancelled-on-start";
    private static readonly LocId SurgeryCantCancelOnStart = "surgery-cant-be-cancelled-on-start";
    private static readonly LocId SurgeryToolFailureDamage = "surgery-tool-damage-on-failure";
    private static readonly LocId SurgeryNeedToBeStarted = "surgery-need-to-be-started-before-operating";
    private static readonly LocId SurgeryPatientFeelsPain = "surgery-patient-feels-pain";

    public override void Initialize()
    {
        base.Initialize();

        InitializeRisk();

        SubscribeLocalEvent<SurgeryPatientComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SurgeryPatientComponent, InteractUsingEvent>(OnSurgeryPatientInteractUsing);
        Subs.BuiEvents<SurgeryPatientComponent>(EdgeSelectorBUIKey, subs =>
        {
            subs.Event<SurgeryEdgeSelectorEdgeSelectedMessage>(OnSurgeryEdgeSelectorEdgeSelectedMessage);
        });

        SubscribeLocalEvent<SurgeryPatientComponent, SurgeryDoAfterEvent>(OnSurgeryDoAfter);
        SubscribeLocalEvent<SurgeryPatientComponent, DoAfterAttemptEvent<SurgeryDoAfterEvent>>((uid, comp, ev) =>
        {
            OnDoAfterAttempt((uid, comp), ev.Event, ev);
        });

        SubscribeLocalEvent<SurgeryStarterComponent, AfterInteractEvent>(OnSurgeryStarterAfterInteract);
        SubscribeLocalEvent<SurgeryStarterComponent, StartSurgeryEvent>(OnStartSurgeryMessage);
    }

    private void OnExamined(Entity<SurgeryPatientComponent> entity, ref ExaminedEvent args)
    {
        foreach (var (surgeryGraphId, node) in entity.Comp.OngoingSurgeries)
        {
            if (!_prototype.Resolve(surgeryGraphId, out var graphProto))
                continue;

            if (!graphProto.TryGetNode(node, out var currentNode))
                continue;

            if (node != null && SurgeryGraph.ExamineDescription(currentNode) != null)
                args.PushMarkup(Loc.GetString(SurgeryGraph.ExamineDescription(currentNode)!), SurgeryExaminePushPriority);
        }
    }

    private void OnSurgeryPatientInteractUsing(Entity<SurgeryPatientComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<SurgeryStarterComponent>(args.Used) && entity.Comp.OngoingSurgeries.Where(x => _prototype.Index(x.Key).Start == x.Value).ToArray() is { Length: > 0 } startSurgeries)
        {
            EndOperation(entity!, startSurgeries[0].Key, args.User);
            return;
        }

        // TODO: maybe allow under heavy painkillers
        if (HasComp<SurgeryToolComponent>(args.Used) && args.User == args.Target)
        {
            _popup.PopupPredictedCursor(Loc.GetString(SurgeryCantPerformOnYourself), args.User);
            return;
        }

        var edgeSelectorState = MakeSelectorState(entity, args.Used, args.User);
        switch (edgeSelectorState.Infos.Count)
        {
            case 0:
                if (!HasComp<SurgeryToolComponent>(args.Used))
                    return;

                if (entity.Comp.OngoingSurgeries.Count == 0)
                    _popup.PopupPredictedCursor(Loc.GetString(SurgeryNeedToBeStarted), args.User);

                foreach (var (surgeryId, _) in entity.Comp.OngoingSurgeries)
                {
                    PopupSurgeryGraphFailures(entity, surgeryId, args.Used, args.User);
                }

                args.Handled = true;
                break;

            case 1:
                var info = edgeSelectorState.Infos[0];

                if (!info.MetEdgeRequirement)
                    return;

                if (GetEdgeTargeting(entity, info.SurgeryProtoId, info.TargetEdgeId) is not { } targetingEdge)
                    return;

                args.Handled = TryPerformOperationStep(entity, info.SurgeryProtoId, targetingEdge, args.Used, args.User);
                break;

            default:
                var metedEdges = edgeSelectorState.Infos.Where(x => x.MetEdgeRequirement).ToArray();
                if (metedEdges.Length == 1)
                {
                    var metedInfo = metedEdges[0];

                    if (GetEdgeTargeting(entity, metedInfo.SurgeryProtoId, metedInfo.TargetEdgeId) is not { } metedEdge)
                        return;

                    args.Handled = TryPerformOperationStep(entity, metedInfo.SurgeryProtoId, metedEdge, args.Used, args.User);
                }
                else
                {
                    // skip showing ui for other interactions
                    if (!HasComp<SurgeryToolComponent>(args.Used))
                        return;

                    var buiOwner = entity.Owner;

                    // We send full state so no reason for ui being at any entity.
                    if (!_userInterface.TryOpenUi(buiOwner, EdgeSelectorBUIKey, args.User))
                        return;

                    _userInterface.SetUiState(buiOwner, EdgeSelectorBUIKey, edgeSelectorState);
                    args.Handled = true;
                }
                break;
        }
    }

    private void OnSurgeryEdgeSelectorEdgeSelectedMessage(Entity<SurgeryPatientComponent> entity, ref SurgeryEdgeSelectorEdgeSelectedMessage args)
    {
        if (GetEdgeTargeting(entity, args.SurgeryId, args.TargetId) is not { } chosenEdge)
            return;

        TryPerformOperationStep(entity, args.SurgeryId, chosenEdge, GetEntity(args.Used), args.Actor);
    }


    private void OnDoAfterAttempt(Entity<SurgeryPatientComponent> entity, SurgeryDoAfterEvent args, CancellableEntityEventArgs ev)
    {
        if (args.Target is null)
            return;

        if (!_prototype.Resolve(args.SurgeryGraph, out var surgeryGraphProto))
        {
            ev.Cancel();
            return;
        }

        if (CanPerformAnyEdgeInSurgery(entity, surgeryGraphProto, args.Used, args.User))
            return;

        PopupSurgeryGraphFailures(entity, surgeryGraphProto, args.Used, args.User);

        ev.Cancel();
        return;
    }

    private void OnSurgeryDoAfter(Entity<SurgeryPatientComponent> entity, ref SurgeryDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            OnSurgeryFailure(args);
            return;
        }

        if (!_prototype.Resolve(args.SurgeryGraph, out var surgeryPrototype))
            return;

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (GetEdgeTargeting(entity, surgeryPrototype, args.EdgeId) is not { } chosenEdge)
            return;

        if (!CanMeetRequirements(entity, chosenEdge, args.Used, args.User))
            return;

        var failChance = GetStepFailureChance(entity.Owner, args.User, args.Used, chosenEdge);
        if (failChance > 0f)
        {
            var rand = SharedRandomExtensions.PredictedRandom(_gameTiming, GetNetEntity(entity.Owner), GetNetEntity(args.User));
            if (rand.NextSingle() < failChance)
            {
                OnSurgeryFailure(args, isStepRollFailure: true);
                return;
            }
        }

        if (!TryMeetRequirement(entity, chosenEdge, args.Used, args.User))
            return;

        ProceedToNextStep(entity, args.User, args.Used, args.SurgeryGraph, chosenEdge);
    }

    private void OnSurgeryStarterAfterInteract(Entity<SurgeryStarterComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !TryComp<SurgeryPatientComponent>(args.Target, out var surgeryPatient))
            return;

        if (args.Target == args.User)
        {
            _popup.PopupPredictedCursor(Loc.GetString(SurgeryCantPerformOnYourself), args.User);
            return;
        }

        if (!_userInterface.HasUi(entity, SurgeryDrapeUiKey.Key))
        {
            Log.Warning($"Entity {ToPrettyString(entity)} has {nameof(SurgeryStarterComponent)} but don't have its UI!");
            return;
        }

        if (!_userInterface.IsUiOpen(entity.Owner, SurgeryDrapeUiKey.Key))
            _userInterface.OpenUi(entity.Owner, SurgeryDrapeUiKey.Key, predicted: true);

        UpdateUserInterface(entity, args.User, args.Target.Value);
        args.Handled = true;
    }

    public void UpdateUserInterface(EntityUid drape, EntityUid user, EntityUid target)
    {
        var netUser = GetNetEntity(user);
        var netTarget = GetNetEntity(target);

        var state = new SurgeryDrapeUpdate(netUser, netTarget);
        _userInterface.SetUiState(drape, SurgeryDrapeUiKey.Key, state);
    }

    private void OnStartSurgeryMessage(Entity<SurgeryStarterComponent> entity, ref StartSurgeryEvent args)
    {
        var (target, user, used) = (GetEntity(args.Target), GetEntity(args.User), GetEntity(args.Used));

        if (target == user)
            return;

        // We have 2 options:
        //   - player wants to stop started surgery
        //   - player wants to start surgery
        // so:
        // 1. get surgery patient comp
        // 2. if surgery is ongoing - trying to end it and return
        // 3. if surgery is not ongoing - try to start it and return

        if (!TryComp<SurgeryPatientComponent>(target, out var surgeryPatientComp))
        {
            args.Cancel();
            return;
        }

        if (surgeryPatientComp.OngoingSurgeries.ContainsKey(args.SurgeryGraphId))
        {
            if (OperationCanBeEnded(target, args.SurgeryGraphId))
            {
                _popup.PopupPredicted(Loc.GetString(SurgeryCancelledOnStart, ("target", args.Target), ("user", args.User)), target, user);
                EndOperation(target, args.SurgeryGraphId, user);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString(SurgeryCantCancelOnStart));
            }

            return;
        }

        if (!CanStartSurgery(target, args.SurgeryGraphId, target, used, out var reason))
        {
            _popup.PopupClient(reason, user, user);
            args.Cancel();
            return;
        }

        if (!TryStartSurgery(target, args.SurgeryGraphId, user, entity))
            return;

        DebugTools.Assert(surgeryPatientComp.OngoingSurgeries.ContainsKey(args.SurgeryGraphId));

        _adminLogManager.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):user} started surgery {args.SurgeryGraphId}) on {ToPrettyString(args.Target):target}!");
    }

    public bool TryStartSurgery(Entity<SurgeryPatientComponent?> target, ProtoId<SurgeryGraphPrototype> surgery, EntityUid performer, EntityUid used)
    {
        if (!Resolve(target.Owner, ref target.Comp, logMissing: false))
            return false;


        if (target.Comp.OngoingSurgeries.ContainsKey(surgery))
            return false;

        DebugTools.Assert(CanStartSurgery(target.Owner, surgery, target, used, out _));

        StartSurgeryNode(target!, surgery, performer, used);

        return true;
    }

    /// <returns> true if operation step performed successful </returns>
    public bool TryPerformOperationStep(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> surgeryGraph, SurgeryGraphEdge chosenEdge, EntityUid? used, EntityUid user)
    {
        if (!CanPerformAnyEdgeInSurgery(entity, surgeryGraph, used, user))
        {
            PopupSurgeryGraphFailures(entity, surgeryGraph, used, user);
            return false;
        }

        var performEdgeInfo = GetPerformSurgeryEdgeInfo(entity, chosenEdge, used, user);
        if (!performEdgeInfo.Visible)
            return false;

        if (performEdgeInfo.FailureReason is not null)
        {
            _popup.PopupPredictedCursor(performEdgeInfo.FailureReason, user);
            return false;
        }

        if (SurgeryGraph.Delay(chosenEdge) is null)
            Log.Fatal($"Found edge {chosenEdge.Id} with zero delay, graph id {surgeryGraph}");

        var secondsDelay = GetStepDelay(entity.Owner, user, used, chosenEdge);

        var performerDoAfterEventArgs =
            new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(secondsDelay),
                new SurgeryDoAfterEvent(surgeryGraph, chosenEdge.Id, chosenEdge.Target), entity.Owner, target: entity.Owner, used: used)
            {
                NeedHand = true,
                BreakOnMove = true,
                MovementThreshold = DoAfterMovementThreshold,
                AttemptFrequency = AttemptFrequency.EveryTick
            };

        if (!_doAfter.TryStartDoAfter(performerDoAfterEventArgs))
            return false;

        if (TryComp<SurgeryToolComponent>(used, out var surgeryTool))
            _audio.PlayPredicted(surgeryTool.UsingSound, entity.Owner, user);

        if (!IsPatientAnesthetized(entity.Owner) && !IsPainlessTool(used))
        {
            _popup.PopupPredicted(
                Loc.GetString(SurgeryPatientFeelsPain, ("target", Identity.Entity(entity.Owner, EntityManager))),
                entity.Owner,
                user,
                PopupType.MediumCaution);
            OnPatientPain(entity.Owner);
        }

        return true;
    }

    private void OnSurgeryFailure(SurgeryDoAfterEvent args, bool isStepRollFailure = false)
    {
        if (args.Target is not { Valid: true } target)
            return;

        var damaged = false;

        if (args.Used is { } usedToolUid && TryComp<SurgeryToolComponent>(usedToolUid, out var surgeryTool) && surgeryTool.FailureDamage is not null)
        {
            if (isStepRollFailure || _gameTiming.CurTime >= surgeryTool.NextFailureDamageTime)
            {
                surgeryTool.NextFailureDamageTime = _gameTiming.CurTime + surgeryTool.FailureDamageDelay;
                Dirty(usedToolUid, surgeryTool);
                _damageable.TryChangeDamage(target, surgeryTool.FailureDamage, origin: args.User);
                damaged = true;
            }
        }
        else if (args.Used is { } usedUid && TryComp<MeleeWeaponComponent>(usedUid, out var meleeWeapon))
        {
            damaged = _meleeWeapon.AttemptLightAttack(args.User, usedUid, meleeWeapon, target, checkCombatMode: false);
        }

        if (!damaged && !isStepRollFailure)
            return;

        if (args.Used is { } usedTool)
        {
            _popup.PopupPredicted(
                Loc.GetString(SurgeryToolFailureDamage, ("used", usedTool), ("target", Identity.Entity(target, EntityManager))),
                target,
                args.User,
                PopupType.SmallCaution);
        }

        _adminLogManager.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):user} failed performing edge of surgery and damaged {ToPrettyString(target):target}!");
    }
}
