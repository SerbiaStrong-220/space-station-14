// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Ui;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] protected SurgeryGraphSystem SurgeryGraph = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ISharedAdminLogManager _adminLogManager = default!;
    [Dependency] private SharedMeleeWeaponSystem _meleeWeapon = default!;
    [Dependency] private MeleeSoundSystem _meleeSound = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
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
    private static readonly LocId SurgeryStepFailed = "surgery-step-failed";
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

            if (SurgeryGraph.ExamineDescription(currentNode) is { } description)
                args.PushMarkup(Loc.GetString(description), SurgeryExaminePushPriority);
        }
    }

    private void OnSurgeryPatientInteractUsing(Entity<SurgeryPatientComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // TODO: maybe allow under heavy painkillers
        if (args.User == args.Target)
        {
            if (HasComp<SurgeryToolComponent>(args.Used))
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
                {
                    if (!IsSurgeryItem(args.Used))
                        return;

                    _popup.PopupPredictedCursor(info.FailureReason, args.User);
                    args.Handled = true;
                    return;
                }

                if (GetEdgeTargeting(entity, info.SurgeryProtoId, info.TargetEdgeId) is not { } targetingEdge)
                    return;

                args.Handled = TryPerformOperationStep(entity, info.SurgeryProtoId, targetingEdge, args.Used, args.User);
                break;

            default:
                EdgeSelectInfo? metedInfo = null;
                var metedCount = 0;
                foreach (var selectInfo in edgeSelectorState.Infos)
                {
                    if (!selectInfo.MetEdgeRequirement)
                        continue;

                    metedInfo = selectInfo;
                    metedCount++;
                }

                if (metedCount == 1 && metedInfo is not null)
                {
                    if (GetEdgeTargeting(entity, metedInfo.SurgeryProtoId, metedInfo.TargetEdgeId) is not { } metedEdge)
                        return;

                    args.Handled = TryPerformOperationStep(entity, metedInfo.SurgeryProtoId, metedEdge, args.Used, args.User);
                }
                else
                {
                    // skip showing ui for other interactions
                    if (!IsSurgeryItem(args.Used) && metedCount == 0)
                        return;

                    var buiOwner = entity.Owner;

                    if (!_gameTiming.IsFirstTimePredicted)
                    {
                        args.Handled = _userInterface.IsUiOpen(buiOwner, EdgeSelectorBUIKey, args.User);
                        break;
                    }

                    // We send full state so no reason for ui being at any entity.
                    if (!_userInterface.TryOpenUi(buiOwner, EdgeSelectorBUIKey, args.User))
                        return;

                    _userInterface.SetUiState(buiOwner, EdgeSelectorBUIKey, edgeSelectorState);
                    args.Handled = true;
                }
                break;
        }
    }

    /// <summary>
    /// Tools and organs exist for surgery, so a failed step with them explains the reason instead of passing the interaction on.
    /// </summary>
    private bool IsSurgeryItem(EntityUid used)
    {
        return HasComp<SurgeryToolComponent>(used) || HasComp<OrganComponent>(used);
    }

    private void OnSurgeryEdgeSelectorEdgeSelectedMessage(Entity<SurgeryPatientComponent> entity, ref SurgeryEdgeSelectorEdgeSelectedMessage args)
    {
        if (!_interaction.InRangeUnobstructed(args.Actor, entity.Owner))
            return;

        EntityUid? used = null;
        if (args.Used is { } netUsed)
        {
            used = GetEntity(netUsed);
            if (!Exists(used) || !_hands.IsHolding(args.Actor, used.Value, out _))
                return;
        }

        if (GetEdgeTargeting(entity, args.SurgeryId, args.TargetId) is not { } chosenEdge)
            return;

        TryPerformOperationStep(entity, args.SurgeryId, chosenEdge, used, args.Actor);
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

        if (!CanPerformAnyEdgeInSurgery(entity, surgeryGraphProto, args.Used, args.User))
        {
            PopupSurgeryGraphFailures(entity, surgeryGraphProto, args.Used, args.User);
            ev.Cancel();
            return;
        }

        if (GetEdgeTargeting(entity, args.SurgeryGraph, args.EdgeId) is not { } edge ||
            !CanMeetRequirements(entity, edge, args.Used, args.User))
        {
            ev.Cancel();
        }
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
        {
            ResyncPatient(entity);
            return;
        }

        var failChance = GetStepFailureChance(entity.Owner, args.User, args.Used, chosenEdge);
        var serverOnly = failChance > 0f;
        if (serverOnly)
        {
            // client and server can finish the DoAfter on different ticks, so a predicted roll would desync
            if (_net.IsClient)
                return;

            if (_random.NextFloat() < failChance)
            {
                OnSurgeryFailure(args, failedEdge: chosenEdge);
                ResyncPatient(entity);
                return;
            }
        }

        if (!TryMeetRequirement(entity, chosenEdge, args.Used, args.User))
        {
            ResyncPatient(entity);
            return;
        }

        ProceedToNextStep(entity, args.User, args.Used, args.SurgeryGraph, chosenEdge, predicted: !serverOnly);
    }

    private void ResyncPatient(Entity<SurgeryPatientComponent> entity)
    {
        if (_net.IsServer)
            Dirty(entity);
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
                _popup.PopupPredicted(
                    Loc.GetString(SurgeryCancelledOnStart,
                        ("target", Identity.Entity(target, EntityManager)),
                        ("user", Identity.Entity(user, EntityManager))),
                    target,
                    user);
                EndOperation(target, args.SurgeryGraphId, user);
            }
            else
            {
                _popup.PopupPredictedCursor(Loc.GetString(SurgeryCantCancelOnStart), user);
            }

            return;
        }

        if (!CanStartSurgery(user, args.SurgeryGraphId, target, used, out var reason))
        {
            _popup.PopupPredictedCursor(reason, user);
            args.Cancel();
            return;
        }

        if (!TryStartSurgery(target, args.SurgeryGraphId, user, entity))
            return;

        DebugTools.Assert(surgeryPatientComp.OngoingSurgeries.ContainsKey(args.SurgeryGraphId));

        _adminLogManager.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):user} started surgery {args.SurgeryGraphId} on {ToPrettyString(target):target}!");
    }

    public bool TryStartSurgery(Entity<SurgeryPatientComponent?> target, ProtoId<SurgeryGraphPrototype> surgery, EntityUid performer, EntityUid used)
    {
        if (!Resolve(target.Owner, ref target.Comp, logMissing: false))
            return false;

        if (target.Comp.OngoingSurgeries.ContainsKey(surgery))
            return false;

        DebugTools.Assert(CanStartSurgery(performer, surgery, target, used, out _));

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
                BreakOnDamage = true,
                MovementThreshold = DoAfterMovementThreshold,
                AttemptFrequency = AttemptFrequency.EveryTick
            };

        if (!_doAfter.TryStartDoAfter(performerDoAfterEventArgs))
            return false;

        if (TryComp<SurgeryToolComponent>(used, out var surgeryTool))
            _audio.PlayPredicted(surgeryTool.UsingSound, entity.Owner, user);

        if (!IsPatientAnesthetized(entity.Owner) && !IsPainlessTool(used) && !SurgeryGraph.IsPainless(chosenEdge))
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

    /// <param name="failedEdge">The step whose roll failed, null if the step was just interrupted</param>
    private void OnSurgeryFailure(SurgeryDoAfterEvent args, SurgeryGraphEdge? failedEdge = null)
    {
        if (_net.IsClient)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        var isStepRollFailure = failedEdge is not null;
        var damaged = false;

        if (args.Used is { } used && TryGetFailureDamage(used, args.User, target, isStepRollFailure, out var failureDamage))
        {
            damaged = _damageable.TryChangeDamage(target, failureDamage, out var appliedDamage, origin: args.User);

            if (damaged && TryComp<MeleeWeaponComponent>(used, out var melee))
                PlayFailureStrike(args.User, (used, melee), target, appliedDamage);
        }

        if (failedEdge is not null && SurgeryGraph.FailureDamage(failedEdge) is { } edgeDamage)
            damaged |= _damageable.TryChangeDamage(target, edgeDamage, out _, origin: args.User);

        if (!damaged && !isStepRollFailure)
            return;

        PopupStepFailure(args, target, damaged);

        _adminLogManager.Add(
            LogType.Action,
            damaged ? LogImpact.Medium : LogImpact.Low,
            $"{ToPrettyString(args.User):user} failed performing edge of surgery{(damaged ? " and damaged" : " on")} {ToPrettyString(target):target}!");
    }

    private bool TryGetFailureDamage(EntityUid used, EntityUid user, EntityUid target, bool isStepRollFailure, [NotNullWhen(true)] out DamageSpecifier? damage)
    {
        damage = null;

        if (TryComp<SurgeryToolComponent>(used, out var surgeryTool) && surgeryTool.FailureDamage is not null)
        {
            if (!isStepRollFailure && _gameTiming.CurTime < surgeryTool.NextFailureDamageTime)
                return false;

            surgeryTool.NextFailureDamageTime = _gameTiming.CurTime + surgeryTool.FailureDamageDelay;
            Dirty(used, surgeryTool);
            damage = surgeryTool.FailureDamage;
            return true;
        }

        if (!TryComp<MeleeWeaponComponent>(used, out var melee))
            return false;

        // a failure with a weapon is a hit, so it obeys the same cooldown and attack restrictions (e.g. pacifism)
        var curTime = _gameTiming.CurTime;
        if (curTime < melee.NextAttack || !_actionBlocker.CanAttack(user, target, (used, melee)))
            return false;

        melee.NextAttack = curTime + TimeSpan.FromSeconds(1f / _meleeWeapon.GetAttackRate(used, user, melee));
        DirtyField(used, melee, nameof(MeleeWeaponComponent.NextAttack));

        damage = _meleeWeapon.GetDamage(used, user, melee);
        return true;
    }

    private void PlayFailureStrike(EntityUid user, Entity<MeleeWeaponComponent> weapon, EntityUid target, DamageSpecifier appliedDamage)
    {
        var userXform = Transform(user);
        var targetPos = _transform.GetMapCoordinates(target).Position;
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));

        if (localPos.LengthSquared() > 0f)
        {
            localPos = userXform.LocalRotation.RotateVec(localPos);

            var visualLength = weapon.Comp.Range - 0.2f;
            if (localPos.Length() > visualLength)
                localPos = localPos.Normalized() * visualLength;

            _meleeWeapon.DoLunge(user, weapon, weapon.Comp.Angle, localPos, weapon.Comp.Animation, predicted: false);
        }

        var damageSound = SharedMeleeWeaponSystem.GetHighestDamageSound(appliedDamage, _prototype);
        _meleeSound.PlayHitSound(target, null, damageSound, null, weapon.Comp);
    }

    private void PopupStepFailure(SurgeryDoAfterEvent args, EntityUid target, bool damaged)
    {
        var message = Loc.GetString(damaged ? SurgeryToolFailureDamage : SurgeryStepFailed,
            ("used", args.Used is { } used ? used : Loc.GetString("surgery-null-used")),
            ("target", Identity.Entity(target, EntityManager)));

        _popup.PopupEntity(message, target, Filter.Pvs(target), true, PopupType.SmallCaution);
    }
}
