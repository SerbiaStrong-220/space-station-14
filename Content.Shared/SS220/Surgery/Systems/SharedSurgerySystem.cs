// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Ui;
using Content.Shared.Examine;
using Robust.Shared.Network;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Database;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] protected readonly SurgeryGraphSystem SurgeryGraph = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _meleeWeapon = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const float ErrorGettingDelayDelay = 8f;
    private const float DoAfterMovementThreshold = 0.15f;
    private const int SurgeryExaminePushPriority = -1;

    private const SurgeryEdgeSelectorUi EdgeSelectorBUIKey = SurgeryEdgeSelectorUi.Key;

    private readonly LocId _surgeryCancelledOnStart = "surgery-cancelled-on-start";
    private readonly LocId _surgeryCantCancelOnStart = "surgery-cant-be-cancelled-on-start";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryPatientComponent, InteractUsingEvent>(OnSurgeryPatientInteractUsing);
        SubscribeLocalEvent<SurgeryPatientComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SurgeryPatientComponent, DoAfterAttemptEvent<SurgeryDoAfterEvent>>((uid, comp, ev) =>
        {
            OnDoAfterAttempt((uid, comp), ev.Event, ev);
        });
        SubscribeLocalEvent<SurgeryPatientComponent, SurgeryDoAfterEvent>(OnSurgeryDoAfter);

        SubscribeLocalEvent<SurgeryStarterComponent, AfterInteractEvent>(OnSurgeryStarterAfterInteract);
        SubscribeLocalEvent<SurgeryStarterComponent, StartSurgeryEvent>(OnStartSurgeryMessage);

        SubscribeLocalEvent<BodyAnalyzerComponent, AfterInteractEvent>(OnBodyAnalyzerAfterInteract);
    }

    private void OnSurgeryPatientInteractUsing(Entity<SurgeryPatientComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var edgeSelectorState = MakeSelectorState(entity, args.Used, args.User);
        switch (edgeSelectorState.Infos.Count)
        {
            case 0:
                foreach (var (surgeryId, _) in entity.Comp.OngoingSurgeries)
                {
                    PopupSurgeryGraphFailures(entity, surgeryId, args.Used, args.User);
                }
                break;

            case 1:
                var info = edgeSelectorState.Infos[0];

                if (GetEdgeTargeting(entity, info.SurgeryProtoId, info.TargetNode) is not { } targetingEdge)
                    return;

                args.Handled = TryPerformOperationStep(entity, info.SurgeryProtoId, targetingEdge, args.Used, args.User);
                break;

            default:

                var buiOwner = entity.Owner;

                // We send full state so no reason for ui being at any entity.
                if (!_userInterface.TryOpenUi(buiOwner, EdgeSelectorBUIKey, entity, predicted: true))
                    return;

                _userInterface.SetUiState(buiOwner, EdgeSelectorBUIKey, edgeSelectorState);
                args.Handled = true;
                break;
        }
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

    private void OnDoAfterAttempt(Entity<SurgeryPatientComponent> _, SurgeryDoAfterEvent args, CancellableEntityEventArgs ev)
    {
        if (args.Target is null)
            return;

        if (!_prototype.Resolve(args.SurgeryGraph, out var surgeryGraph))
            return;

        foreach (var requirement in surgeryGraph.Requirements)
        {
            var requirementTarget = ResolveRequirementSubject(requirement, args.User, args.Target, args.Used);

            if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                continue;

            var reason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);

            _popup.PopupClient(reason, args.User, args.User);

            if (TryComp<MeleeWeaponComponent>(args.Used, out var meleeWeapon))
                _meleeWeapon.AttemptLightAttack(args.User, args.Used.Value, meleeWeapon, args.Target.Value, checkCombatMode: false);

            ev.Cancel();
            return;
        }
    }

    private void OnSurgeryDoAfter(Entity<SurgeryPatientComponent> entity, ref SurgeryDoAfterEvent args)
    {
        if (args.Cancelled || !entity.Comp.OngoingSurgeries.TryGetValue(args.SurgeryGraph, out var currentNode))
            return;

        var operationProto = _prototype.Index(args.SurgeryGraph);
        if (!operationProto.TryGetNode(currentNode, out var node))
            return;

        SurgeryGraphEdge? targetEdge = null;
        foreach (var edge in node.Edges)
        {
            if (edge.Target != args.TargetEdge)
                continue;

            targetEdge = edge;
            break;
        }

        if (targetEdge == null)
        {
            if (_netManager.IsServer)
                Log.Error($"Got wrong target edge [{args.TargetEdge}] in surgery do after for graph [{args.SurgeryGraph}]!");

            return;
        }

        ProceedToNextStep(entity, args.User, args.Used, args.SurgeryGraph, targetEdge);
    }

    private void OnSurgeryStarterAfterInteract(Entity<SurgeryStarterComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !TryComp<SurgeryPatientComponent>(args.Target, out var surgeryPatient))
            return;

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

    private void OnBodyAnalyzerAfterInteract(Entity<BodyAnalyzerComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        if (!_userInterface.HasUi(entity, BodyAnalyzerUiKey.Key))
        {
            Log.Debug($"Entity {ToPrettyString(entity)} has {nameof(BodyAnalyzerComponent)} but don't have its UI!");
            return;
        }

        _userInterface.OpenUi(entity.Owner, BodyAnalyzerUiKey.Key, args.User);

        var netTarget = GetNetEntity(args.Target.Value);

        var state = new BodyAnalyzerTargetUpdate(netTarget);
        _userInterface.SetUiState(entity.Owner, BodyAnalyzerUiKey.Key, state);
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
                _popup.PopupPredicted(Loc.GetString(_surgeryCancelledOnStart, ("target", args.Target), ("user", args.User)), target, user);
                EndOperation(target, args.SurgeryGraphId, user);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString(_surgeryCantCancelOnStart));
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

        if (SurgeryGraph.Delay(chosenEdge) is not { } secondsDelay)
        {
            Log.Fatal($"Found edge {chosenEdge} with zero delay, graph id {surgeryGraph}");
            secondsDelay = ErrorGettingDelayDelay;
        }

        var ev = new GetSurgeryDelayModifiersEvent();
        RaiseLocalEvent(entity, ref ev);
        RaiseLocalEvent(user, ref ev);

        if (used is not null)
            RaiseLocalEvent(used.Value, ref ev);

        secondsDelay *= ev.Multiplier;
        secondsDelay += ev.FlatModifier;

        var performerDoAfterEventArgs =
            new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(secondsDelay),
                new SurgeryDoAfterEvent(surgeryGraph, chosenEdge.Target), entity.Owner, target: entity.Owner, used: used)
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

        return true;
    }
}
