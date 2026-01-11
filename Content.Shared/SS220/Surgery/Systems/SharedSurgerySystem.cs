// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
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
using Content.Shared.Mobs;
using System.Linq;
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
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    private const float ErrorGettingDelayDelay = 8f;
    private const float DoAfterMovementThreshold = 0.15f;
    private const int SurgeryExaminePushPriority = -1;

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
        SubscribeLocalEvent<BodyAnalyzerComponent, AfterInteractEvent>(OnBodyAnalyzerAfterInteract);
        SubscribeLocalEvent<SurgeryStarterComponent, StartSurgeryEvent>(OnStartSurgeryMessage);
    }

    private void OnSurgeryPatientInteractUsing(Entity<SurgeryPatientComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // here hardcoded one operation at a time (TODO: maybe add radial menu for possible interactions?)
        var surgeryGraphId = entity.Comp.OngoingSurgeries.FirstOrNull()?.Key;

        if (surgeryGraphId is null)
            return;

        args.Handled = TryPerformOperationStep(entity, surgeryGraphId.Value, args.Used, args.User);
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
            Log.Debug($"Entity {ToPrettyString(entity)} has {nameof(SurgeryStarterComponent)} but don't have its UI!");
            return;
        }

        // no operation case
        if (surgeryPatient.OngoingSurgeries.Count == 0)
        {
            if (!_userInterface.IsUiOpen(entity.Owner, SurgeryDrapeUiKey.Key))
                _userInterface.OpenUi(entity.Owner, SurgeryDrapeUiKey.Key, predicted: true);

            UpdateUserInterface(entity, args.User, args.Target.Value);
            return;
        }

        // TODO:
        // So idea is:
        // - we make 2 layer radial menu
        //   - first choose surgery
        //   - second choose edge
        // Other code is kinda okayish

        // here hardcoded one operation at a time (TODO: maybe add radial menu for possible interactions?)
        var nullableSurgeryGraphId = surgeryPatient.OngoingSurgeries.FirstOrNull()?.Key;

        if (nullableSurgeryGraphId is not { } surgeryGraphId)
            return;

        if (OperationCanBeEnded(args.Target.Value, surgeryGraphId))
        {
            _adminLogManager.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(args.User):user}  stopped surgery {surgeryGraphId} on {ToPrettyString(args.Target):target}");

            _popup.PopupPredicted(Loc.GetString("surgery-cancelled", ("target", args.Target), ("user", args.User)), args.Target.Value, args.User);
            EndOperation(args.Target.Value, surgeryGraphId);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("surgery-cant-be-cancelled"));
        }
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
        var target = GetEntity(args.Target);
        var user = GetEntity(args.User);
        var used = GetEntity(args.Used);

        // TODO: make opening on yourself unavailable by drapes
        if (target == user)
            return;

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
    public bool TryPerformOperationStep(Entity<SurgeryPatientComponent> entity, ProtoId<SurgeryGraphPrototype> surgeryGraph, EntityUid? used, EntityUid user)
    {
        if (!_prototype.Resolve(surgeryGraph, out var graphProto))
            return false;

        if (!entity.Comp.OngoingSurgeries.TryGetValue(surgeryGraph, out var currentNodeName))
        {
            Log.Error($"Tried to perform operation step for surgery [{surgeryGraph}] but {ToPrettyString(entity)} don't have that surgery!");
            return false;
        }

        foreach (var requirement in graphProto.Requirements)
        {
            var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

            if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                continue;

            var reason = requirement.RequirementFailureReason(requirementTarget, _prototype, EntityManager);

            _popup.PopupClient(reason, user, user);
        }

        if (!graphProto.TryGetNode(currentNodeName, out var currentNode))
        {
            Log.Fatal($"Current node of {ToPrettyString(entity)} has incorrect value {currentNodeName} for graph proto {surgeryGraph}");
            return false;
        }

        SurgeryGraphEdge? chosenEdge = null;
        foreach (var edge in currentNode.Edges)
        {
            bool visible = true;
            foreach (var requirement in SurgeryGraph.GetVisibilityRequirements(edge))
            {
                var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

                if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                    continue;

                visible = false;
                break;
            }

            if (!visible)
                continue;

            // id any edges exist make it true
            bool isAbleToPerform = true;
            foreach (var requirement in SurgeryGraph.GetRequirements(edge))
            {
                var requirementTarget = ResolveRequirementSubject(requirement, user, entity.Owner, used);

                if (requirement.SatisfiesRequirements(requirementTarget, EntityManager))
                    continue;

                isAbleToPerform = false;
                break;
            }

            // if passed all conditions than break
            if (isAbleToPerform)
            {
                chosenEdge = edge;
                break;
            }
        }

        if (chosenEdge == null)
            return false;

        // lets be honest, I don't believe that everyone will check their's surgeryGraphPrototype mapping
        var delay = SurgeryGraph.Delay(chosenEdge);
        if (delay == null)
        {
            Log.Fatal($"Found edge [{chosenEdge}] with zero delay, graph id [{surgeryGraph}]");
            delay = ErrorGettingDelayDelay;
        }

        var ev = new GetSurgeryDelayModifiersEvent();
        RaiseLocalEvent(entity, ref ev);
        RaiseLocalEvent(user, ref ev);

        if (used is not null)
            RaiseLocalEvent(used.Value, ref ev);

        delay *= ev.Multiplier;
        delay += ev.FlatModifier;

        var performerDoAfterEventArgs =
            new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(delay.Value),
                            new SurgeryDoAfterEvent(surgeryGraph, chosenEdge.Target), entity.Owner, target: entity.Owner, used: used)
            {
                NeedHand = true,
                BreakOnMove = true,
                MovementThreshold = DoAfterMovementThreshold,
                AttemptFrequency = AttemptFrequency.EveryTick
            };

        if (_doAfter.TryStartDoAfter(performerDoAfterEventArgs) && TryComp<SurgeryToolComponent>(used, out var surgeryTool))
            _audio.PlayPredicted(surgeryTool.UsingSound, entity.Owner, user);

        return true;
    }
}
